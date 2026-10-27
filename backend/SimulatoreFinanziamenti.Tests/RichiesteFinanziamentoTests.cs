using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SimulatoreFinanziamenti.Tests;

public class RichiesteFinanziamentoTests : IDisposable
{
    private readonly string _database = Path.Combine(Path.GetTempPath(), $"simulatore-{Guid.NewGuid():N}.db");
    private readonly WebApplicationFactory<Program> _factory;

    public RichiesteFinanziamentoTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Production");
            b.UseSetting("ConnectionStrings:Simulatore", $"Data Source={_database};Pooling=False");
            b.UseSetting("AreaBanca:Password", "segreta");
        });
    }

    public void Dispose()
    {
        _factory.Dispose();
        File.Delete(_database);
    }

    private static readonly object Simulazione = new { finalita = "Auto", importo = 15_000, durataMesi = 48, etaRichiedente = 35, provincia = "RM" };

    private static object Cliente(string codiceFiscale = "rssmra85t10a562s", bool consenso = true) => new
    {
        nome = " Mario ",
        cognome = "Rossi",
        codiceFiscale,
        email = "mario.rossi@example.com",
        telefono = "+39 333 1234567",
        consensoPrivacy = consenso
    };

    private async Task<int> IdProdotto(HttpClient client, string codice)
    {
        var risultato = await (await client.PostAsJsonAsync("/api/simulazioni", Simulazione)).Content.ReadFromJsonAsync<JsonElement>();
        return risultato.GetProperty("offerte").EnumerateArray().Single(o => o.GetProperty("codice").GetString() == codice).GetProperty("prodottoId").GetInt32();
    }

    [Fact]
    public async Task Invio_RestituisceIlNumeroDiPraticaEIlTracciatoStandard()
    {
        var client = _factory.CreateClient();
        var id = await IdProdotto(client, "PRE-AUTO");

        var risposta = await client.PostAsJsonAsync("/api/richieste-finanziamento", new { prodottoId = id, simulazione = Simulazione, cliente = Cliente() });

        Assert.Equal(HttpStatusCode.OK, risposta.StatusCode);
        var esito = await risposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Matches(@"^PF-\d{8}-\d{6}$", esito.GetProperty("numeroPratica").GetString());
        Assert.Equal("Richiesta inviata con successo", esito.GetProperty("messaggio").GetString());

        var pratica = esito.GetProperty("pratica");
        Assert.Equal("1.0", pratica.GetProperty("versione").GetString());
        Assert.Equal("PRE-AUTO", pratica.GetProperty("prodotto").GetProperty("codice").GetString());
        Assert.Equal("Mario", pratica.GetProperty("cliente").GetProperty("nome").GetString());
        Assert.Equal("RSSMRA85T10A562S", pratica.GetProperty("cliente").GetProperty("codiceFiscale").GetString());
        Assert.Equal(15_000m, pratica.GetProperty("finanziamento").GetProperty("importo").GetDecimal());
        Assert.True(pratica.GetProperty("finanziamento").GetProperty("taeg").GetDecimal() > 0);
        Assert.Equal(48, pratica.GetProperty("pianoAmmortamento").GetArrayLength());
    }

    [Fact]
    public async Task ProdottoNonCompatibile_NonVieneInviato()
    {
        var client = _factory.CreateClient();
        var id = await IdProdotto(client, "PRE-AUTO");
        var altraFinalita = new { finalita = "Liquidita", importo = 15_000, durataMesi = 48, etaRichiedente = 35 };

        var risposta = await client.PostAsJsonAsync("/api/richieste-finanziamento", new { prodottoId = id, simulazione = altraFinalita, cliente = Cliente() });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, risposta.StatusCode);
    }

    [Theory]
    [InlineData("RSSMRA85T10A56", true)]
    [InlineData("RSSMRA85T10A562S", false)]
    public async Task DatiClienteNonValidi_Rifiutati(string codiceFiscale, bool consenso)
    {
        var client = _factory.CreateClient();
        var id = await IdProdotto(client, "PRE-AUTO");

        var risposta = await client.PostAsJsonAsync("/api/richieste-finanziamento", new { prodottoId = id, simulazione = Simulazione, cliente = Cliente(codiceFiscale, consenso) });

        Assert.Equal(HttpStatusCode.BadRequest, risposta.StatusCode);
    }

    [Fact]
    public async Task ContrattoOpenApi_Pubblicato()
    {
        var documento = await _factory.CreateClient().GetStringAsync("/openapi/v1.json");

        Assert.Contains("/api/richieste-finanziamento", documento);
        Assert.Contains("PraticaFinanziamento", documento);
    }
}
