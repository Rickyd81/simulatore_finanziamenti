using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace SimulatoreFinanziamenti.Tests;

public class PricingTests : IDisposable
{
    private readonly string _database = Path.Combine(Path.GetTempPath(), $"simulatore-{Guid.NewGuid():N}.db");
    private WebApplicationFactory<Program> _factory;

    public PricingTests() => _factory = Crea();

    private WebApplicationFactory<Program> Crea() => new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
    {
        b.UseEnvironment("Production");
        b.UseSetting("ConnectionStrings:Simulatore", $"Data Source={_database};Pooling=False");
        b.UseSetting("AreaBanca:Password", "segreta");
    });

    public void Dispose()
    {
        _factory.Dispose();
        File.Delete(_database);
    }

    private async Task<HttpClient> ClientBanca()
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/accesso", new { password = "segreta" })).StatusCode);
        return client;
    }

    private static async Task<List<JsonElement>> Pricing(HttpClient client) =>
        [.. (await client.GetFromJsonAsync<JsonElement>("/api/pricing")).EnumerateArray()];

    private static JsonElement PerCodice(List<JsonElement> elenco, string codice) =>
        elenco.Single(p => p.GetProperty("codice").GetString() == codice);

    private static Task<HttpResponseMessage> NuovoProdottoVariabile(HttpClient client, string? pricing, decimal? spread = null) =>
        client.PostAsJsonAsync("/api/prodotti", new
        {
            codice = "VAR-TEST",
            nome = "Mutuo variabile di prova",
            tipologia = "Mutuo",
            tipoTasso = "Variabile",
            codicePricing = pricing,
            spread,
            finalitaAmmesse = new[] { "Liquidita" },
            importoMinimo = 10_000,
            importoMassimo = 200_000,
            durataMinimaMesi = 60,
            durataMassimaMesi = 360,
            etaMinima = 18,
            provinceAmmesse = Array.Empty<string>(),
            attivo = true,
            dataInizioValidita = "2026-01-01"
        });

    [Fact]
    public async Task Elenco_ContienePiuPricingDiEsempioConSpread()
    {
        var elenco = await Pricing(await ClientBanca());

        Assert.True(elenco.Count > 1);
        Assert.All(elenco, p => Assert.Equal(JsonValueKind.Number, p.GetProperty("spread").ValueKind));
        Assert.Equal(1.20m, PerCodice(elenco, "EUR3M").GetProperty("spread").GetDecimal());
    }

    [Fact]
    public async Task Salva_RichiedeLoSpread()
    {
        var client = await ClientBanca();

        var senza = await client.PutAsJsonAsync("/api/pricing/IRS10", new { codice = "IRS10", descrizione = "IRS 10 anni", valore = 2.4, dataRilevazione = "2026-10-01" });
        Assert.Equal(HttpStatusCode.BadRequest, senza.StatusCode);

        var con = await client.PutAsJsonAsync("/api/pricing/irs10", new { codice = "IRS10", descrizione = "IRS 10 anni", valore = 2.4, spread = 1.3, dataRilevazione = "2026-10-01" });
        Assert.Equal(HttpStatusCode.OK, con.StatusCode);
        Assert.Equal(1.3m, PerCodice(await Pricing(client), "IRS10").GetProperty("spread").GetDecimal());
    }

    [Fact]
    public async Task Prodotto_RichiamandoIlPricingNeImportaLoSpread()
    {
        var client = await ClientBanca();

        var risposta = await NuovoProdottoVariabile(client, "eur6m");

        Assert.Equal(HttpStatusCode.Created, risposta.StatusCode);
        var prodotto = await risposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("EUR6M", prodotto.GetProperty("codicePricing").GetString());
        Assert.Equal(1.10m, prodotto.GetProperty("spread").GetDecimal());

        // Lo spread importato è una copia: cambiare il pricing non tocca il prodotto.
        await client.PutAsJsonAsync("/api/pricing/EUR6M", new { codice = "EUR6M", descrizione = "Euribor 6 mesi", valore = 2.12, spread = 2.0, dataRilevazione = "2026-10-01" });
        var salvato = await client.GetFromJsonAsync<JsonElement>($"/api/prodotti/{prodotto.GetProperty("id").GetInt32()}");
        Assert.Equal(1.10m, salvato.GetProperty("spread").GetDecimal());
    }

    [Fact]
    public async Task Prodotto_SpreadModificatoSulProdottoVieneMantenuto()
    {
        var client = await ClientBanca();

        var prodotto = await (await NuovoProdottoVariabile(client, "EUR6M", spread: 0.75m)).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(0.75m, prodotto.GetProperty("spread").GetDecimal());
    }

    [Fact]
    public async Task Prodotto_RichiedeUnPricingEsistente()
    {
        var client = await ClientBanca();

        Assert.Equal(HttpStatusCode.BadRequest, (await NuovoProdottoVariabile(client, null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await NuovoProdottoVariabile(client, "INESISTENTE")).StatusCode);
    }

    [Fact]
    public async Task Simulazione_UsaLoSpreadImportatoSenzaCondizioniDiTasso()
    {
        var client = await ClientBanca();
        Assert.Equal(HttpStatusCode.Created, (await NuovoProdottoVariabile(client, "EUR1M")).StatusCode);

        var risultato = await (await client.PostAsJsonAsync("/api/simulazioni", new
        {
            finalita = "Liquidita",
            importo = 100_000,
            durataMesi = 120,
            etaRichiedente = 40,
            dataDecorrenza = "2026-11-01"
        })).Content.ReadFromJsonAsync<JsonElement>();

        var offerta = risultato.GetProperty("offerte").EnumerateArray().Single(o => o.GetProperty("codice").GetString() == "VAR-TEST");
        Assert.Equal("EUR1M", offerta.GetProperty("indiceRiferimento").GetString());
        Assert.Equal(1.40m, offerta.GetProperty("spread").GetDecimal());
        Assert.Equal(1.95m + 1.40m, offerta.GetProperty("tan").GetDecimal());
    }

    [Fact]
    public async Task DatabaseEsistente_IndiciDiventanoPricingSenzaPerdereDati()
    {
        // Database di una versione precedente: indici di riferimento senza spread, un indice personalizzato.
        await _factory.CreateClient().GetAsync("/api/tabelle");
        _factory.Dispose();
        using (var connessione = new SqliteConnection($"Data Source={_database};Pooling=False"))
        {
            connessione.Open();
            using var comando = connessione.CreateCommand();
            comando.CommandText = """
                DELETE FROM Indici WHERE Codice <> 'EUR3M';
                INSERT INTO Indici (Codice, Descrizione, Valore, DataRilevazione) VALUES ('MIO', 'Indice personalizzato', '3.1', '2026-09-01');
                UPDATE Indici SET Valore = '2.50' WHERE Codice = 'EUR3M';
                ALTER TABLE Indici DROP COLUMN Spread;
                ALTER TABLE Prodotti DROP COLUMN Spread;
                """;
            comando.ExecuteNonQuery();
        }

        _factory = Crea();
        var client = await ClientBanca();

        var elenco = await Pricing(client);
        var eur3m = PerCodice(elenco, "EUR3M");
        Assert.Equal(2.50m, eur3m.GetProperty("valore").GetDecimal()); // il valore rilevato resta quello salvato
        Assert.Equal(1.20m, eur3m.GetProperty("spread").GetDecimal());
        Assert.Equal(3.1m, PerCodice(elenco, "MIO").GetProperty("valore").GetDecimal());
        Assert.Equal(JsonValueKind.Null, PerCodice(elenco, "MIO").GetProperty("spread").ValueKind);
        Assert.Contains(elenco, p => p.GetProperty("codice").GetString() == "EUR6M");

        var prodotti = (await client.GetFromJsonAsync<JsonElement>("/api/prodotti")).EnumerateArray().ToList();
        var variabile = prodotti.Single(p => p.GetProperty("codice").GetString() == "MUT-VAR");
        Assert.Equal("EUR3M", variabile.GetProperty("codicePricing").GetString());
    }
}
