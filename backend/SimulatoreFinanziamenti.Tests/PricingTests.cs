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
    private static async Task<int> NuovoProdotto(HttpClient client, string tipoTasso)
    {
        var risposta = await client.PostAsJsonAsync("/api/prodotti", new
        {
            codice = "TEST",
            nome = "Mutuo di prova",
            tipologia = "Mutuo",
            tipoTasso,
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
        Assert.Equal(HttpStatusCode.Created, risposta.StatusCode);
        return (await risposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private static Task<HttpResponseMessage> NuovaCondizioneTasso(HttpClient client, int prodotto, string? pricing, decimal spread, decimal? tasso = null) =>
        client.PostAsJsonAsync($"/api/prodotti/{prodotto}/condizioni", new
        {
            codice = "TAN1",
            categoria = "Tasso",
            descrizione = "TAN",
            tipoValore = "Percentuale",
            codicePricing = pricing,
            tassoRiferimento = tasso,
            valore = spread,
            periodicita = "Annuale",
            dataInizioValidita = "2026-01-01"
        });

    private static async Task<JsonElement> Offerta(HttpClient client, string codice = "TEST", string finalita = "Liquidita", int importo = 100_000)
    {
        var risultato = await (await client.PostAsJsonAsync("/api/simulazioni", new
        {
            finalita,
            importo,
            valoreImmobile = finalita == "AcquistoPrimaCasa" ? 300_000 : (int?)null,
            durataMesi = 300,
            etaRichiedente = 40,
            provincia = "RM",
            dataDecorrenza = "2026-11-01"
        })).Content.ReadFromJsonAsync<JsonElement>();
        return risultato.GetProperty("offerte").EnumerateArray().Single(o => o.GetProperty("codice").GetString() == codice);
    }


    [Fact]
    public async Task Elenco_ContienePiuPricingDiEsempioConSpread()
    {
        var elenco = await Pricing(await ClientBanca());

        Assert.True(elenco.Count > 1);
        Assert.All(elenco, p => Assert.Equal(JsonValueKind.Number, p.GetProperty("spread").ValueKind));
        Assert.Equal(1.20m, PerCodice(elenco, "EUR3M").GetProperty("spread").GetDecimal());
    }

    [Fact]
    public async Task Elenco_ContienePricingFissiEVariabili()
    {
        var elenco = await Pricing(await ClientBanca());

        Assert.Equal("Fisso", PerCodice(elenco, "IRS20").GetProperty("tipoTasso").GetString());
        Assert.Equal("Variabile", PerCodice(elenco, "EUR3M").GetProperty("tipoTasso").GetString());
        Assert.True(elenco.Count(p => p.GetProperty("tipoTasso").GetString() == "Fisso") > 1);
        Assert.True(elenco.Count(p => p.GetProperty("tipoTasso").GetString() == "Variabile") > 1);
    }

    [Fact]
    public async Task Salva_RichiedeIlTipoDiTasso()
    {
        var client = await ClientBanca();

        var senza = await client.PutAsJsonAsync("/api/pricing/IRS15", new { codice = "IRS15", descrizione = "IRS 15 anni", valore = 2.4, spread = 1.3, dataRilevazione = "2026-10-01" });
        Assert.Equal(HttpStatusCode.BadRequest, senza.StatusCode);

        // Un pricing già richiamato da condizioni di prodotti non può cambiare tipo di tasso.
        var cambio = await client.PutAsJsonAsync("/api/pricing/EUR3M", new { codice = "EUR3M", descrizione = "Euribor 3 mesi", tipoTasso = "Fisso", valore = 2.05, spread = 1.2, dataRilevazione = "2026-10-01" });
        Assert.Equal(HttpStatusCode.BadRequest, cambio.StatusCode);
    }

    [Fact]
    public async Task Prodotto_HaSoloIlTipoDiTassoSenzaPricing()
    {
        var client = await ClientBanca();
        var id = await NuovoProdotto(client, "Variabile");

        var prodotto = await client.GetFromJsonAsync<JsonElement>($"/api/prodotti/{id}");
        Assert.False(prodotto.TryGetProperty("codicePricing", out _));
        Assert.False(prodotto.TryGetProperty("spread", out _));
    }

    [Fact]
    public async Task CondizioneTasso_RichiamandoIlPricingNeImportaIlTasso()
    {
        var client = await ClientBanca();
        var prodotto = await NuovoProdotto(client, "Fisso");

        var risposta = await NuovaCondizioneTasso(client, prodotto, "irs10", spread: 0.90m);

        Assert.Equal(HttpStatusCode.Created, risposta.StatusCode);
        var condizione = await risposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("IRS10", condizione.GetProperty("codicePricing").GetString());
        Assert.Equal(2.30m, condizione.GetProperty("tassoRiferimento").GetDecimal());
        Assert.Equal(3.20m, condizione.GetProperty("tan").GetDecimal());

        // Tasso e spread sono una copia: aggiornare il pricing non cambia la condizione.
        await client.PutAsJsonAsync("/api/pricing/IRS10", new { codice = "IRS10", descrizione = "IRS 10 anni", tipoTasso = "Fisso", valore = 3.0, spread = 0.9, dataRilevazione = "2026-11-01" });
        var offerta = await Offerta(client);
        Assert.Equal("IRS10", offerta.GetProperty("indiceRiferimento").GetString());
        Assert.Equal(2.30m, offerta.GetProperty("valoreIndice").GetDecimal());
        Assert.Equal(0.90m, offerta.GetProperty("spread").GetDecimal());
        Assert.Equal(3.20m, offerta.GetProperty("tan").GetDecimal());
    }

    [Fact]
    public async Task CondizioneTasso_TassoESpreadSonoModificabili()
    {
        var client = await ClientBanca();
        var prodotto = await NuovoProdotto(client, "Variabile");

        Assert.Equal(HttpStatusCode.Created, (await NuovaCondizioneTasso(client, prodotto, "EUR1M", spread: 1.0m, tasso: 2.10m)).StatusCode);

        Assert.Equal(3.10m, (await Offerta(client)).GetProperty("tan").GetDecimal());
    }

    [Fact]
    public async Task CondizioneTasso_RichiedeUnPricingEsistenteDelTipoDiTassoDelProdotto()
    {
        var client = await ClientBanca();
        var fisso = await NuovoProdotto(client, "Fisso");

        Assert.Equal(HttpStatusCode.BadRequest, (await NuovaCondizioneTasso(client, fisso, null, 1m)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await NuovaCondizioneTasso(client, fisso, "INESISTENTE", 1m)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await NuovaCondizioneTasso(client, fisso, "EUR3M", 1m)).StatusCode);
    }

    [Fact]
    public async Task Importazione_RifiutaTassiConPricingDiAltroTipo()
    {
        var client = await ClientBanca();
        var variabile = await NuovoProdotto(client, "Variabile");
        var anagrafica = (await client.GetFromJsonAsync<JsonElement>("/api/anagrafica-condizioni")).EnumerateArray();
        var tassoFisso = anagrafica.First(c => c.GetProperty("codice").GetString() == "MF-TAN1").GetProperty("id").GetInt32();

        var risposta = await client.PostAsJsonAsync($"/api/prodotti/{variabile}/condizioni/importa", new { condizioniAnagrafica = new[] { tassoFisso } });

        Assert.Equal(HttpStatusCode.BadRequest, risposta.StatusCode);
    }

    [Fact]
    public async Task CondizioneNonDiTasso_NonHaPricing()
    {
        var client = await ClientBanca();
        var prodotto = await NuovoProdotto(client, "Fisso");

        var risposta = await client.PostAsJsonAsync($"/api/prodotti/{prodotto}/condizioni", new
        {
            codice = "BOL", categoria = "ImpostaBollo", descrizione = "Bollo", tipoValore = "Importo", valore = 16,
            codicePricing = "IRS10", tassoRiferimento = 2.3, periodicita = "UnaTantum", dataInizioValidita = "2026-01-01"
        });

        var condizione = await risposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, condizione.GetProperty("codicePricing").ValueKind);
        Assert.Equal(JsonValueKind.Null, condizione.GetProperty("tan").ValueKind);
    }


    [Fact]
    public async Task Salva_RichiedeLoSpread()
    {
        var client = await ClientBanca();

        var senza = await client.PutAsJsonAsync("/api/pricing/IRS15", new { codice = "IRS15", descrizione = "IRS 15 anni", tipoTasso = "Fisso", valore = 2.4, dataRilevazione = "2026-10-01" });
        Assert.Equal(HttpStatusCode.BadRequest, senza.StatusCode);

        var con = await client.PutAsJsonAsync("/api/pricing/irs15", new { codice = "IRS15", descrizione = "IRS 15 anni", tipoTasso = "Fisso", valore = 2.4, spread = 1.3, dataRilevazione = "2026-10-01" });
        Assert.Equal(HttpStatusCode.OK, con.StatusCode);
        Assert.Equal(1.3m, PerCodice(await Pricing(client), "IRS15").GetProperty("spread").GetDecimal());
    }


    [Fact]
    public async Task DatabaseEsistente_PricingPassaAlleCondizioniSenzaCambiareITan()
    {
        // Database delle versioni precedenti: indici senza spread né tipo, pricing e spread sul prodotto,
        // condizioni di tasso senza pricing (spread per il variabile, TAN per il fisso).
        var client0 = await ClientBanca();
        var tanFisso = (await Offerta(client0, "MUT-FIX", "AcquistoPrimaCasa", 200_000)).GetProperty("tan").GetDecimal();
        _factory.Dispose();
        using (var connessione = new SqliteConnection($"Data Source={_database};Pooling=False"))
        {
            connessione.Open();
            using var comando = connessione.CreateCommand();
            comando.CommandText = """
                DELETE FROM Indici WHERE Codice NOT IN ('EUR3M', 'TBP');
                INSERT INTO Indici (Codice, Descrizione, Valore, DataRilevazione) VALUES ('MIO', 'Indice personalizzato', '3.1', '2026-09-01');
                UPDATE Indici SET Valore = '2.50' WHERE Codice = 'EUR3M';
                ALTER TABLE Indici DROP COLUMN Spread;
                ALTER TABLE Indici DROP COLUMN TipoTasso;
                UPDATE Condizioni SET Valore = '3.4' WHERE Codice = 'TAN4' AND ProdottoId = (SELECT Id FROM Prodotti WHERE Codice = 'MUT-FIX');
                UPDATE Prodotti SET IndiceRiferimento = 'EUR3M' WHERE Codice = 'MUT-VAR';
                DELETE FROM Condizioni WHERE Categoria = 'Tasso' AND ProdottoId = (SELECT Id FROM Prodotti WHERE Codice = 'PRE-AUTO');
                UPDATE Prodotti SET IndiceRiferimento = 'TBP', Spread = '2.40' WHERE Codice = 'PRE-AUTO';
                ALTER TABLE Condizioni DROP COLUMN CodicePricing;
                ALTER TABLE Condizioni DROP COLUMN TassoRiferimento;
                ALTER TABLE CondizioniAnagrafica DROP COLUMN CodicePricing;
                ALTER TABLE CondizioniAnagrafica DROP COLUMN TassoRiferimento;
                """;
            comando.ExecuteNonQuery();
        }

        _factory = Crea();
        var client = await ClientBanca();

        // Anagrafica pricing: valori conservati, indici precedenti variabili, esempi mancanti aggiunti.
        var elenco = await Pricing(client);
        Assert.Equal(2.50m, PerCodice(elenco, "EUR3M").GetProperty("valore").GetDecimal());
        Assert.Equal(1.20m, PerCodice(elenco, "EUR3M").GetProperty("spread").GetDecimal());
        Assert.Equal("Variabile", PerCodice(elenco, "MIO").GetProperty("tipoTasso").GetString());
        Assert.Equal("Fisso", PerCodice(elenco, "IRS20").GetProperty("tipoTasso").GetString());

        // Variabile: le condizioni ricevono il pricing del prodotto con il valore di oggi dell'indice.
        var variabile = await Offerta(client, "MUT-VAR", "AcquistoPrimaCasa", 200_000);
        Assert.Equal("EUR3M", variabile.GetProperty("indiceRiferimento").GetString());
        Assert.Equal(2.50m + 1.20m, variabile.GetProperty("tan").GetDecimal());
        // Fisso senza pricing: pricing TF-PREC a zero, il TAN resta quello della condizione.
        var fisso = await Offerta(client, "MUT-FIX", "AcquistoPrimaCasa", 200_000);
        Assert.Equal("TF-PREC", fisso.GetProperty("indiceRiferimento").GetString());
        Assert.Equal(tanFisso, fisso.GetProperty("tan").GetDecimal());
        // Spread sul prodotto senza condizioni di tasso: diventa una condizione di tasso.
        var condizioniAuto = (await client.GetFromJsonAsync<JsonElement>($"/api/prodotti/{(await client.GetFromJsonAsync<JsonElement>("/api/prodotti")).EnumerateArray().Single(p => p.GetProperty("codice").GetString() == "PRE-AUTO").GetProperty("id").GetInt32()}/condizioni")).EnumerateArray();
        var tassoAuto = condizioniAuto.Single(c => c.GetProperty("categoria").GetString() == "Tasso");
        Assert.Equal("TBP", tassoAuto.GetProperty("codicePricing").GetString());
        Assert.Equal(5.90m, tassoAuto.GetProperty("tan").GetDecimal());
        // Le condizioni di tasso dell'anagrafica senza pricing ricevono TF-PREC.
        var anagrafica = (await client.GetFromJsonAsync<JsonElement>("/api/anagrafica-condizioni")).EnumerateArray();
        Assert.All(anagrafica.Where(c => c.GetProperty("categoria").GetString() == "Tasso"),
            c => Assert.Equal("TF-PREC", c.GetProperty("codicePricing").GetString()));
    }
}
