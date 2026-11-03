using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace SimulatoreFinanziamenti.Api.Data;

/// <summary>
/// Sceglie il database dalla stringa di connessione "Simulatore":
/// PostgreSQL (es. Neon) se è un URL postgres:// o postgresql:// oppure contiene Host=, altrimenti SQLite.
/// </summary>
public static class ConfigurazioneDatabase
{
    public const string SqliteLocale = "Data Source=simulatore.db";

    public static DbContextOptionsBuilder Usa(DbContextOptionsBuilder opzioni, string? connessione)
    {
        connessione = string.IsNullOrWhiteSpace(connessione) ? SqliteLocale : connessione.Trim();
        return IsPostgres(connessione)
            ? opzioni.UseNpgsql(DaUrl(connessione))
            : opzioni.UseSqlite(connessione);
    }

    public static bool IsPostgres(string connessione) =>
        connessione.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        || connessione.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
        || connessione.Contains("Host=", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Neon e gli altri servizi forniscono l'URL postgresql://utente:password@host/database?sslmode=require,
    /// che Npgsql non accetta: lo converte nel formato chiave=valore. Le altre stringhe restano invariate.
    /// </summary>
    public static string DaUrl(string connessione)
    {
        if (!connessione.Contains("://")) return connessione;

        var url = new Uri(connessione);
        var credenziali = url.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = url.Host,
            Port = url.IsDefaultPort || url.Port <= 0 ? 5432 : url.Port,
            Database = Uri.UnescapeDataString(url.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credenziali[0]),
            Password = credenziali.Length > 1 ? Uri.UnescapeDataString(credenziali[1]) : null,
            // Un database nel cloud si raggiunge sempre cifrato, salvo indicazione diversa nell'URL.
            SslMode = SslMode.Require,
        };

        foreach (var parte in url.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var chiaveValore = parte.Split('=', 2);
            var chiave = Uri.UnescapeDataString(chiaveValore[0]);
            var valore = chiaveValore.Length > 1 ? Uri.UnescapeDataString(chiaveValore[1]) : "";
            switch (chiave.ToLowerInvariant())
            {
                case "sslmode":
                    builder.SslMode = Enum.Parse<SslMode>(valore.Replace("-", ""), ignoreCase: true);
                    break;
                case "channel_binding":
                    builder.ChannelBinding = Enum.Parse<ChannelBinding>(valore, ignoreCase: true);
                    break;
                default:
                    // Parametri già nel formato Npgsql (es. Timeout=30); quelli sconosciuti si ignorano.
                    if (builder.ContainsKey(chiave)) builder[chiave] = valore;
                    break;
            }
        }
        return builder.ConnectionString;
    }
}
