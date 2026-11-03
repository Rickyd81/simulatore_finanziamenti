using Microsoft.EntityFrameworkCore;
using Npgsql;
using SimulatoreFinanziamenti.Api.Data;

namespace SimulatoreFinanziamenti.Tests;

public class ConfigurazioneDatabaseTests
{
    [Fact]
    public void UrlNeonConvertitoInStringaNpgsql()
    {
        var connessione = new NpgsqlConnectionStringBuilder(ConfigurazioneDatabase.DaUrl(
            "postgresql://utente:p%40ss%3Aw0rd@ep-cool-name-123456.eu-central-1.aws.neon.tech/neondb?sslmode=require&channel_binding=require"));

        Assert.Equal("ep-cool-name-123456.eu-central-1.aws.neon.tech", connessione.Host);
        Assert.Equal(5432, connessione.Port);
        Assert.Equal("neondb", connessione.Database);
        Assert.Equal("utente", connessione.Username);
        Assert.Equal("p@ss:w0rd", connessione.Password);
        Assert.Equal(SslMode.Require, connessione.SslMode);
        Assert.Equal(ChannelBinding.Require, connessione.ChannelBinding);
    }

    [Fact]
    public void UrlSenzaSslmodeUsaConnessioneCifrata()
    {
        var connessione = new NpgsqlConnectionStringBuilder(ConfigurazioneDatabase.DaUrl("postgres://u:p@db.example.com:6543/simulatore"));

        Assert.Equal(6543, connessione.Port);
        Assert.Equal(SslMode.Require, connessione.SslMode);
    }

    [Fact]
    public void StringaChiaveValoreRestaInvariata()
    {
        const string stringa = "Host=localhost;Database=simulatore;Username=postgres;Password=x";
        Assert.Equal(stringa, ConfigurazioneDatabase.DaUrl(stringa));
    }

    [Theory]
    [InlineData("postgresql://u:p@host/db", true)]
    [InlineData("postgres://u:p@host/db", true)]
    [InlineData("Host=localhost;Database=db", true)]
    [InlineData("Data Source=simulatore.db", false)]
    [InlineData("Data Source=/app/data/simulatore.db", false)]
    public void ProviderSceltoDallaStringaDiConnessione(string stringa, bool postgres)
    {
        Assert.Equal(postgres, ConfigurazioneDatabase.IsPostgres(stringa));

        var opzioni = new DbContextOptionsBuilder<SimulatoreDbContext>();
        ConfigurazioneDatabase.Usa(opzioni, stringa);
        using var db = new SimulatoreDbContext(opzioni.Options);
        Assert.Equal(postgres, db.Database.IsNpgsql());
        Assert.Equal(!postgres, db.Database.IsSqlite());
    }
}
