using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace SimulatoreFinanziamenti.Tests;

public class AnagraficaCondizioniTests : IDisposable
{
    private readonly string _database = Path.Combine(Path.GetTempPath(), $"simulatore-{Guid.NewGuid():N}.db");
    private WebApplicationFactory<Program> _factory;

    public AnagraficaCondizioniTests() => _factory = Crea();

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

    private static object Modello(string codice, string? gruppo = "Test") => new
    {
        codice,
        gruppo,
        categoria = "SpesaIstruttoria",
        descrizione = "Istruttoria di prova",
        tipoValore = "Percentuale",
        valore = 1.5,
        importoMinimo = 400,
        periodicita = "UnaTantum",
        inclusaNelTaeg = true,
        dataInizioValidita = "2026-01-01"
    };

    private static async Task<int> NuovoProdotto(HttpClient client)
    {
        var risposta = await client.PostAsJsonAsync("/api/prodotti", new
        {
            codice = "TEST",
            nome = "Prodotto di prova",
            tipologia = "PrestitoPersonale",
            tipoTasso = "Fisso",
            finalitaAmmesse = new[] { "Liquidita" },
            importoMinimo = 1_000,
            importoMassimo = 10_000,
            durataMinimaMesi = 12,
            durataMassimaMesi = 60,
            etaMinima = 18,
            provinceAmmesse = Array.Empty<string>(),
            attivo = true,
            dataInizioValidita = "2026-01-01"
        });
        Assert.Equal(HttpStatusCode.Created, risposta.StatusCode);
        return (await risposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private static async Task<List<JsonElement>> Anagrafica(HttpClient client) =>
        [.. (await client.GetFromJsonAsync<JsonElement>("/api/anagrafica-condizioni")).EnumerateArray()];

    private static int Id(List<JsonElement> elenco, string codice) =>
        elenco.Single(c => c.GetProperty("codice").GetString() == codice).GetProperty("id").GetInt32();

    [Fact]
    public async Task Anagrafica_RichiedeLAccessoAllAreaBanca()
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/anagrafica-condizioni")).StatusCode);
    }

    [Fact]
    public async Task Anagrafica_ContieneLeCondizioniDiEsempioRaggruppate()
    {
        var elenco = await Anagrafica(await ClientBanca());

        Assert.Contains(elenco, c => c.GetProperty("codice").GetString() == "IST" && c.GetProperty("gruppo").GetString() == "Costi mutuo");
        Assert.Contains(elenco, c => c.GetProperty("codice").GetString() == "MF-SPR1" && c.GetProperty("categoria").GetString() == "Tasso");
    }

    [Fact]
    public async Task Anagrafica_InserisceModificaEdElimina()
    {
        var client = await ClientBanca();

        var inserita = await client.PostAsJsonAsync("/api/anagrafica-condizioni", Modello(" nuova "));
        Assert.Equal(HttpStatusCode.Created, inserita.StatusCode);
        var id = (await inserita.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.Equal("NUOVA", (await client.GetFromJsonAsync<JsonElement>($"/api/anagrafica-condizioni/{id}")).GetProperty("codice").GetString());

        var doppia = await client.PostAsJsonAsync("/api/anagrafica-condizioni", Modello("NUOVA"));
        Assert.Equal(HttpStatusCode.BadRequest, doppia.StatusCode);
        Assert.Contains("Esiste già", await doppia.Content.ReadAsStringAsync());

        var aggiornata = await client.PutAsJsonAsync($"/api/anagrafica-condizioni/{id}", Modello("NUOVA", gruppo: "  "));
        Assert.Equal(HttpStatusCode.OK, aggiornata.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await aggiornata.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("gruppo").ValueKind);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/anagrafica-condizioni/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/anagrafica-condizioni/{id}")).StatusCode);
    }

    [Fact]
    public async Task Anagrafica_ApplicaLeStesseRegoleDelCondizioniere()
    {
        var client = await ClientBanca();
        var tassoInEuro = new { codice = "T", categoria = "Tasso", descrizione = "x", tipoValore = "Importo", valore = 3, periodicita = "Annuale", dataInizioValidita = "2026-01-01" };

        var risposta = await client.PostAsJsonAsync("/api/anagrafica-condizioni", tassoInEuro);

        Assert.Equal(HttpStatusCode.BadRequest, risposta.StatusCode);
        Assert.Contains("percentuale", await risposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Importa_CopiaLeCondizioniNelProdottoChePoiSiPossonoModificare()
    {
        var client = await ClientBanca();
        var prodotto = await NuovoProdotto(client);
        var anagrafica = await Anagrafica(client);

        var risposta = await client.PostAsJsonAsync($"/api/prodotti/{prodotto}/condizioni/importa",
            new { condizioniAnagrafica = new[] { Id(anagrafica, "MF-SPR1"), Id(anagrafica, "IST-PRE"), Id(anagrafica, "BOL") } });

        Assert.Equal(HttpStatusCode.OK, risposta.StatusCode);
        var importate = (await risposta.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        Assert.Equal(["MF-SPR1", "IST-PRE", "BOL"], importate.Select(c => c.GetProperty("codice").GetString()));
        Assert.All(importate, c => Assert.Equal(c.GetProperty("codice").GetString(), c.GetProperty("codiceAnagrafica").GetString()));
        var tasso = importate[0];
        Assert.Equal(0.50m, tasso.GetProperty("valore").GetDecimal());
        Assert.Equal(240, tasso.GetProperty("fasciaDurataAMesi").GetInt32());

        // La condizione importata si modifica sul prodotto senza toccare l'anagrafica.
        var modifica = JsonSerializer.Deserialize<Dictionary<string, object?>>(tasso.GetRawText())!;
        modifica["valore"] = 4.10m;
        modifica["fasciaLtvA"] = null; // un prestito non ha LTV
        modifica["codiceAnagrafica"] = null;
        var aggiornata = await client.PutAsJsonAsync($"/api/prodotti/{prodotto}/condizioni/{tasso.GetProperty("id").GetInt32()}", modifica);
        Assert.Equal(HttpStatusCode.OK, aggiornata.StatusCode);
        var corpo = await aggiornata.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(4.10m, corpo.GetProperty("valore").GetDecimal());
        Assert.Equal("MF-SPR1", corpo.GetProperty("codiceAnagrafica").GetString());

        var modello = await client.GetFromJsonAsync<JsonElement>($"/api/anagrafica-condizioni/{Id(anagrafica, "MF-SPR1")}");
        Assert.Equal(0.50m, modello.GetProperty("valore").GetDecimal());

        // Il prodotto con le condizioni importate è simulabile.
        var simulazione = await (await client.PostAsJsonAsync("/api/simulazioni",
            new { finalita = "Liquidita", importo = 5_000, durataMesi = 36, etaRichiedente = 40, provincia = "RM" })).Content.ReadFromJsonAsync<JsonElement>();
        var offerta = simulazione.GetProperty("offerte").EnumerateArray().Single(o => o.GetProperty("codice").GetString() == "TEST");
        Assert.Equal(4.10m, offerta.GetProperty("tan").GetDecimal());
    }

    [Fact]
    public async Task Importa_RifiutaICodiciGiaPresentiNelProdotto()
    {
        var client = await ClientBanca();
        var prodotto = await NuovoProdotto(client);
        var bollo = Id(await Anagrafica(client), "BOL");
        var importa = new { condizioniAnagrafica = new[] { bollo } };
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/prodotti/{prodotto}/condizioni/importa", importa)).StatusCode);

        var seconda = await client.PostAsJsonAsync($"/api/prodotti/{prodotto}/condizioni/importa", importa);

        Assert.Equal(HttpStatusCode.BadRequest, seconda.StatusCode);
        Assert.Contains("BOL", await seconda.Content.ReadAsStringAsync());
        var condizioni = await client.GetFromJsonAsync<JsonElement>($"/api/prodotti/{prodotto}/condizioni");
        Assert.Equal(1, condizioni.GetArrayLength());
    }

    [Fact]
    public async Task InserimentoManuale_NonHaOrigineDallAnagrafica()
    {
        var client = await ClientBanca();
        var prodotto = await NuovoProdotto(client);

        var risposta = await client.PostAsJsonAsync($"/api/prodotti/{prodotto}/condizioni", new
        {
            codice = "TAN1", categoria = "Tasso", descrizione = "TAN", tipoValore = "Percentuale", valore = 6, periodicita = "Annuale",
            codiceAnagrafica = "FINTO", dataInizioValidita = "2026-01-01"
        });

        Assert.Equal(HttpStatusCode.Created, risposta.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await risposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codiceAnagrafica").ValueKind);
    }

    [Fact]
    public async Task DatabaseEsistente_VieneAggiornatoConLAnagrafica()
    {
        // Database creato da una versione precedente: senza anagrafica né origine delle condizioni.
        await _factory.CreateClient().GetAsync("/api/tabelle");
        _factory.Dispose();
        using (var connessione = new SqliteConnection($"Data Source={_database};Pooling=False"))
        {
            connessione.Open();
            using var comando = connessione.CreateCommand();
            comando.CommandText = "DROP TABLE CondizioniAnagrafica; ALTER TABLE Condizioni DROP COLUMN CodiceAnagrafica;";
            comando.ExecuteNonQuery();
        }

        _factory = Crea();
        var client = await ClientBanca();

        var anagrafica = await Anagrafica(client);
        Assert.NotEmpty(anagrafica);
        var prodotto = await NuovoProdotto(client);
        var risposta = await client.PostAsJsonAsync($"/api/prodotti/{prodotto}/condizioni/importa", new { condizioniAnagrafica = new[] { Id(anagrafica, "BOL") } });
        Assert.Equal(HttpStatusCode.OK, risposta.StatusCode);
        // I prodotti già presenti restano intatti.
        Assert.Contains("PRE-AUTO", await client.GetStringAsync("/api/prodotti"));
    }
}
