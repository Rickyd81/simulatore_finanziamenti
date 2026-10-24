using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SimulatoreFinanziamenti.Tests;

public class AccessoTests : IDisposable
{
    private readonly string _database = Path.Combine(Path.GetTempPath(), $"simulatore-{Guid.NewGuid():N}.db");
    private readonly WebApplicationFactory<Program> _factory;

    public AccessoTests()
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

    [Fact]
    public async Task AreaBanca_RichiedeLaPassword()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/prodotti")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/accesso", new { password = "sbagliata" })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/accesso", new { password = "segreta" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/prodotti")).StatusCode);

        await client.DeleteAsync("/api/accesso");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/indici")).StatusCode);
    }

    [Fact]
    public async Task Simulatore_Resta_Pubblico()
    {
        var client = _factory.CreateClient();
        var risposta = await client.PostAsJsonAsync("/api/simulazioni",
            new { finalita = "Auto", importo = 15_000, durataMesi = 48, etaRichiedente = 35 });

        Assert.Equal(HttpStatusCode.OK, risposta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/tabelle")).StatusCode);
    }
}
