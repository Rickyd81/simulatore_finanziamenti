using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace SimulatoreFinanziamenti.Api.Data;

/// <summary>
/// Allinea un database SQLite creato da una versione precedente al modello attuale:
/// crea le tabelle mancanti (con i relativi indici) e aggiunge le colonne nullable mancanti.
/// Non modifica né cancella mai tabelle o colonne esistenti.
/// </summary>
public static class AggiornamentoSchema
{
    /// <returns>I nomi delle tabelle create.</returns>
    public static HashSet<string> Applica(SimulatoreDbContext db)
    {
        var connessione = db.Database.GetDbConnection();
        var daChiudere = connessione.State != System.Data.ConnectionState.Open;
        if (daChiudere) connessione.Open();
        try
        {
            var esistenti = Elenco(connessione, "SELECT name FROM sqlite_master WHERE type = 'table'");
            var create = new HashSet<string>();
            // Lo script di creazione completo, diviso in istruzioni: si eseguono solo quelle delle tabelle mancanti.
            var istruzioni = db.Database.GenerateCreateScript().ReplaceLineEndings("\n")
                .Split(";\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var entita in db.Model.GetEntityTypes())
            {
                var tabella = entita.GetTableName();
                if (tabella is null) continue;

                if (!esistenti.Contains(tabella))
                {
                    foreach (var sql in istruzioni.Where(i => i.StartsWith($"CREATE TABLE \"{tabella}\"") || i.Contains($" ON \"{tabella}\" ")))
                        Esegui(connessione, sql);
                    create.Add(tabella);
                    continue;
                }

                var colonne = Elenco(connessione, $"SELECT name FROM pragma_table_info('{tabella}')");
                var tabellaStore = StoreObjectIdentifier.Table(tabella, null);
                foreach (var proprieta in entita.GetProperties())
                {
                    var colonna = proprieta.GetColumnName(tabellaStore);
                    if (colonna is null || colonne.Contains(colonna)) continue;
                    if (!proprieta.IsNullable)
                        throw new InvalidOperationException($"Impossibile aggiungere la colonna obbligatoria {tabella}.{colonna} a un database esistente");
                    Esegui(connessione, $"ALTER TABLE \"{tabella}\" ADD COLUMN \"{colonna}\" {proprieta.GetColumnType()} NULL");
                }
            }
            return create;
        }
        finally
        {
            if (daChiudere) connessione.Close();
        }
    }

    private static HashSet<string> Elenco(DbConnection connessione, string sql)
    {
        using var comando = connessione.CreateCommand();
        comando.CommandText = sql;
        using var lettore = comando.ExecuteReader();
        var nomi = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (lettore.Read()) nomi.Add(lettore.GetString(0));
        return nomi;
    }

    private static void Esegui(DbConnection connessione, string sql)
    {
        using var comando = connessione.CreateCommand();
        comando.CommandText = sql;
        comando.ExecuteNonQuery();
    }
}
