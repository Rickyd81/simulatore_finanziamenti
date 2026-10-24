using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SimulatoreFinanziamenti.Api.Data;
using SimulatoreFinanziamenti.Api.Domain;
using SimulatoreFinanziamenti.Api.Dtos;
using SimulatoreFinanziamenti.Api.Services;

namespace SimulatoreFinanziamenti.Tests;

public class MotoreSimulazioneTests : IDisposable
{
    private static readonly DateOnly Decorrenza = new(2026, 11, 1);
    private readonly SqliteConnection _connessione = new("Data Source=:memory:");
    private readonly SimulatoreDbContext _db;

    public MotoreSimulazioneTests()
    {
        _connessione.Open();
        _db = new SimulatoreDbContext(new DbContextOptionsBuilder<SimulatoreDbContext>().UseSqlite(_connessione).Options);
        _db.Database.EnsureCreated();
        DatiIniziali.Carica(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connessione.Dispose();
    }

    private RisultatoSimulazione Simula(RichiestaSimulazione richiesta)
    {
        var prodotti = _db.Prodotti.Include(p => p.Condizioni).AsNoTracking().ToList();
        return new MotoreSimulazione().Simula(richiesta with { DataDecorrenza = Decorrenza }, prodotti, _db.Indici.AsNoTracking().ToList());
    }

    private static RichiestaSimulazione MutuoPrimaCasa(int eta = 40, string provincia = "RM") => new()
    {
        Finalita = Finalita.AcquistoPrimaCasa,
        Importo = 200_000,
        ValoreImmobile = 300_000,
        DurataMesi = 300,
        EtaRichiedente = eta,
        Provincia = provincia
    };

    [Fact]
    public void MutuoPrimaCasa_ProponeFissoEVariabile()
    {
        var risultato = Simula(MutuoPrimaCasa());

        Assert.Equal(66.67m, risultato.Ltv);
        Assert.Equal(["MUT-FIX", "MUT-VAR"], risultato.Offerte.Select(o => o.Codice).Order());

        var fisso = risultato.Offerte.Single(o => o.Codice == "MUT-FIX");
        Assert.Equal(3.40m, fisso.Tan); // oltre 20 anni e LTV oltre il 60%
        Assert.True(fisso.Taeg > fisso.Tan);
        Assert.Contains(fisso.Costi, c => c.Codice == "IMP1" && c.Importo == 500m);
        Assert.DoesNotContain(fisso.Costi, c => c.Codice == "IMP2");
        Assert.Equal(1_500m, fisso.Costi.Single(c => c.Codice == "IST").Importo); // 1% con massimo 1.500 €

        var variabile = risultato.Offerte.Single(o => o.Codice == "MUT-VAR");
        Assert.Equal(2.05m + 1.20m, variabile.Tan);
        Assert.Equal(1.20m, variabile.Spread);

        // Il mutuo giovani è escluso per età, il mutuo Lombardia per provincia.
        Assert.Contains(risultato.NonDisponibili, n => n.Nome.Contains("Giovani"));
        Assert.Contains(risultato.NonDisponibili, n => n.Nome.Contains("Lombardia"));
    }

    [Fact]
    public void MutuoGiovani_AmmessoSottoI36AnniFinoAl100Percento()
    {
        var risultato = Simula(MutuoPrimaCasa(eta: 30, provincia: "MI") with { Importo = 290_000 });

        Assert.Equal(["MUT-GIO"], risultato.Offerte.Select(o => o.Codice));
        Assert.Contains(risultato.NonDisponibili, n => n.Motivi.Any(m => m.Contains("valore dell'immobile")));
    }

    [Fact]
    public void SecondaCasa_ApplicaImpostaAl2Percento()
    {
        var risultato = Simula(MutuoPrimaCasa() with { Finalita = Finalita.AcquistoSecondaCasa });
        var fisso = risultato.Offerte.Single(o => o.Codice == "MUT-FIX");

        Assert.Equal(4_000m, fisso.Costi.Single(c => c.Codice == "IMP2").Importo);
    }

    [Fact]
    public void EtaAFineFinanziamento_EscludeIlProdotto()
    {
        var risultato = Simula(MutuoPrimaCasa(eta: 60));

        Assert.Empty(risultato.Offerte);
        Assert.All(risultato.NonDisponibili.Where(n => !n.Nome.Contains("Giovani") && !n.Nome.Contains("Lombardia")),
            n => Assert.Contains(n.Motivi, m => m.Contains("80 anni")));
    }

    [Fact]
    public void PrestitoPersonale_FasciaDiImporto()
    {
        var piccolo = Simula(new RichiestaSimulazione { Finalita = Finalita.Liquidita, Importo = 10_000, DurataMesi = 60, EtaRichiedente = 35 });
        var grande = Simula(new RichiestaSimulazione { Finalita = Finalita.Liquidita, Importo = 20_000, DurataMesi = 60, EtaRichiedente = 35 });

        Assert.Equal(7.90m, piccolo.Offerte.Single().Tan);
        Assert.Equal(6.90m, grande.Offerte.Single().Tan);
        Assert.Equal(60, piccolo.Offerte.Single().PianoAmmortamento.Count);
    }

    [Fact]
    public void ProdottiDiAltreFinalitaNonCompaiono()
    {
        var risultato = Simula(new RichiestaSimulazione { Finalita = Finalita.Auto, Importo = 15_000, DurataMesi = 48, EtaRichiedente = 35 });

        Assert.Equal(["PRE-AUTO"], risultato.Offerte.Select(o => o.Codice));
        Assert.Empty(risultato.NonDisponibili);
    }

    [Fact]
    public void Condizione_FasciaLtvRichiedeLtv()
    {
        var condizione = new Condizione { FasciaLtvA = 60, DataInizioValidita = new DateOnly(2026, 1, 1) };
        var richiesta = new RichiestaSimulazione { Finalita = Finalita.Liquidita, Importo = 1000, DurataMesi = 12, EtaRichiedente = 30 };

        Assert.False(MotoreSimulazione.Applicabile(condizione, richiesta, null, Decorrenza));
        Assert.True(MotoreSimulazione.Applicabile(condizione, richiesta, 50, Decorrenza));
    }

    [Fact]
    public void MutuoPerLiquidita_NonRichiedeIlValoreDellImmobile()
    {
        // Prodotto di tipo mutuo con finalità liquidità, come configurato in fabbrica prodotti:
        // il simulatore per la liquidità non raccoglie il valore dell'immobile.
        _db.Prodotti.Add(new Prodotto
        {
            Codice = "MUT-LIQ",
            Nome = "Mutuo Liquidità",
            Tipologia = TipologiaProdotto.Mutuo,
            TipoTasso = TipoTasso.Fisso,
            FinalitaAmmesse = [Finalita.Liquidita],
            ImportoMinimo = 10_000,
            ImportoMassimo = 300_000,
            DurataMinimaMesi = 60,
            DurataMassimaMesi = 240,
            EtaMassimaFineFinanziamento = 80,
            LtvMassimo = 50,
            DataInizioValidita = new DateOnly(2026, 1, 1),
            Condizioni = [new Condizione { Codice = "TAN1", Categoria = CategoriaCondizione.Tasso, Descrizione = "TAN", TipoValore = TipoValore.Percentuale, Valore = 4.5m, DataInizioValidita = new DateOnly(2026, 1, 1) }]
        });
        _db.SaveChanges();

        var risultato = Simula(new RichiestaSimulazione { Finalita = Finalita.Liquidita, Importo = 50_000, DurataMesi = 120, EtaRichiedente = 40 });

        Assert.Contains(risultato.Offerte, o => o.Codice == "MUT-LIQ");
        Assert.DoesNotContain(risultato.NonDisponibili, n => n.Nome == "Mutuo Liquidità");
    }

    [Fact]
    public void MutuoAcquisto_SenzaValoreImmobile_RestaNonDisponibile()
    {
        var risultato = Simula(MutuoPrimaCasa() with { ValoreImmobile = null });

        Assert.Empty(risultato.Offerte);
        Assert.Contains(risultato.NonDisponibili, n => n.Motivi.Contains("Indicare il valore dell'immobile"));
    }
}
