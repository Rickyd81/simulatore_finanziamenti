using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SimulatoreFinanziamenti.Api.Domain;

namespace SimulatoreFinanziamenti.Api.Data;

public class SimulatoreDbContext(DbContextOptions<SimulatoreDbContext> options) : DbContext(options)
{
    public DbSet<Prodotto> Prodotti => Set<Prodotto>();
    public DbSet<Condizione> Condizioni => Set<Condizione>();
    public DbSet<IndiceRiferimento> Indici => Set<IndiceRiferimento>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite non ha un tipo decimal nativo: lo salviamo come testo per non perdere precisione.
        configurationBuilder.Properties<decimal>().HaveConversion<string>();
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Prodotto>(e =>
        {
            e.HasIndex(p => p.Codice).IsUnique();
            e.Property(p => p.FinalitaAmmesse).HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Enum.Parse<Finalita>).ToList(),
                ListComparer<Finalita>());
            e.Property(p => p.ProvinceAmmesse).HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                ListComparer<string>());
            e.HasMany(p => p.Condizioni).WithOne(c => c.Prodotto).HasForeignKey(c => c.ProdottoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Condizione>().HasIndex(c => new { c.ProdottoId, c.Codice }).IsUnique();
    }

    private static ValueComparer<List<T>> ListComparer<T>() => new(
        (a, b) => a!.SequenceEqual(b!),
        v => v.Aggregate(0, (h, x) => HashCode.Combine(h, x!.GetHashCode())),
        v => v.ToList());
}
