using SimulatoreFinanziamenti.Api.Domain;

namespace SimulatoreFinanziamenti.Api.Data;

/// <summary>Catalogo di esempio caricato al primo avvio, quando il database è vuoto.</summary>
public static class DatiIniziali
{
    private static readonly DateOnly Inizio = new(2026, 1, 1);

    public static void Carica(SimulatoreDbContext db)
    {
        if (db.Prodotti.Any()) return;

        db.Indici.Add(new IndiceRiferimento { Codice = "EUR3M", Descrizione = "Euribor 3 mesi", Valore = 2.05m, DataRilevazione = new DateOnly(2026, 10, 1) });

        db.Prodotti.AddRange(
            MutuoFisso(), MutuoVariabile(), MutuoGiovani(), MutuoTerritorio(),
            PrestitoLiquidita(), PrestitoAuto(), PrestitoGreen());

        db.SaveChanges();
    }

    private static Prodotto Mutuo(string codice, string nome, string descrizione, TipoTasso tipoTasso, params Finalita[] finalita) => new()
    {
        Codice = codice,
        Nome = nome,
        Descrizione = descrizione,
        Tipologia = TipologiaProdotto.Mutuo,
        TipoTasso = tipoTasso,
        IndiceRiferimento = tipoTasso == TipoTasso.Variabile ? "EUR3M" : null,
        FinalitaAmmesse = [.. finalita],
        ImportoMinimo = 50_000,
        ImportoMassimo = 1_000_000,
        DurataMinimaMesi = 60,
        DurataMassimaMesi = 360,
        EtaMinima = 18,
        EtaMassimaFineFinanziamento = 80,
        LtvMassimo = 80,
        DataInizioValidita = Inizio
    };

    private static List<Condizione> CostiMutuo(decimal istruttoria = 1m) =>
    [
        new() { Codice = "IST", Categoria = CategoriaCondizione.SpesaIstruttoria, Descrizione = "Spese di istruttoria", TipoValore = TipoValore.Percentuale, Valore = istruttoria, ImportoMinimo = istruttoria > 0 ? 500 : null, ImportoMassimo = istruttoria > 0 ? 1_500 : null, Periodicita = Periodicita.UnaTantum, DataInizioValidita = Inizio },
        new() { Codice = "PER", Categoria = CategoriaCondizione.SpesaPerizia, Descrizione = "Perizia tecnica dell'immobile", TipoValore = TipoValore.Importo, Valore = 300, Periodicita = Periodicita.UnaTantum, DataInizioValidita = Inizio },
        new() { Codice = "INC", Categoria = CategoriaCondizione.CommissioneIncassoRata, Descrizione = "Commissione incasso rata (addebito in conto)", TipoValore = TipoValore.Importo, Valore = 2, Periodicita = Periodicita.Mensile, DataInizioValidita = Inizio },
        new() { Codice = "ASS", Categoria = CategoriaCondizione.Assicurazione, Descrizione = "Polizza incendio e scoppio obbligatoria", TipoValore = TipoValore.Importo, Valore = 120, Periodicita = Periodicita.Annuale, DataInizioValidita = Inizio },
        new() { Codice = "IMP1", Categoria = CategoriaCondizione.ImpostaSostitutiva, Descrizione = "Imposta sostitutiva prima casa (0,25%)", TipoValore = TipoValore.Percentuale, Valore = 0.25m, Periodicita = Periodicita.UnaTantum, Finalita = Finalita.AcquistoPrimaCasa, DataInizioValidita = Inizio },
        new() { Codice = "IMP2", Categoria = CategoriaCondizione.ImpostaSostitutiva, Descrizione = "Imposta sostitutiva seconda casa (2%)", TipoValore = TipoValore.Percentuale, Valore = 2m, Periodicita = Periodicita.UnaTantum, Finalita = Finalita.AcquistoSecondaCasa, DataInizioValidita = Inizio }
    ];

    private static Condizione Tasso(string codice, string descrizione, decimal valore, int? durataDa = null, int? durataA = null, decimal? ltvDa = null, decimal? ltvA = null, decimal? importoDa = null, decimal? importoA = null) => new()
    {
        Codice = codice,
        Categoria = CategoriaCondizione.Tasso,
        Descrizione = descrizione,
        TipoValore = TipoValore.Percentuale,
        Valore = valore,
        Periodicita = Periodicita.Annuale,
        FasciaDurataDaMesi = durataDa,
        FasciaDurataAMesi = durataA,
        FasciaLtvDa = ltvDa,
        FasciaLtvA = ltvA,
        FasciaImportoDa = importoDa,
        FasciaImportoA = importoA,
        DataInizioValidita = Inizio
    };

    private static Prodotto MutuoFisso()
    {
        var p = Mutuo("MUT-FIX", "Mutuo Casa Tasso Fisso", "Rata costante per tutta la durata, per acquisto della casa o surroga da altra banca.",
            TipoTasso.Fisso, Finalita.AcquistoPrimaCasa, Finalita.AcquistoSecondaCasa, Finalita.Surroga);
        p.Condizioni =
        [
            Tasso("TAN1", "TAN fisso fino a 20 anni, LTV fino al 60%", 2.95m, durataA: 240, ltvA: 60),
            Tasso("TAN2", "TAN fisso fino a 20 anni, LTV oltre il 60%", 3.15m, durataA: 240, ltvDa: 60),
            Tasso("TAN3", "TAN fisso oltre 20 anni, LTV fino al 60%", 3.20m, durataDa: 241, ltvA: 60),
            Tasso("TAN4", "TAN fisso oltre 20 anni, LTV oltre il 60%", 3.40m, durataDa: 241, ltvDa: 60),
            .. CostiMutuo()
        ];
        return p;
    }

    private static Prodotto MutuoVariabile()
    {
        var p = Mutuo("MUT-VAR", "Mutuo Casa Tasso Variabile", "Tasso legato all'Euribor 3 mesi più uno spread fisso.",
            TipoTasso.Variabile, Finalita.AcquistoPrimaCasa, Finalita.AcquistoSecondaCasa, Finalita.Surroga);
        p.Condizioni =
        [
            Tasso("SPR1", "Spread su Euribor 3M, LTV fino al 60%", 0.95m, ltvA: 60),
            Tasso("SPR2", "Spread su Euribor 3M, LTV oltre il 60%", 1.20m, ltvDa: 60),
            .. CostiMutuo()
        ];
        return p;
    }

    private static Prodotto MutuoGiovani()
    {
        var p = Mutuo("MUT-GIO", "Mutuo Giovani Under 36", "Per l'acquisto della prima casa da parte di chi non ha ancora compiuto 36 anni, finanziabile fino al 100% del valore con garanzia del Fondo Consap.",
            TipoTasso.Fisso, Finalita.AcquistoPrimaCasa);
        p.EtaMassimaRichiedente = 35;
        p.LtvMassimo = 100;
        p.ImportoMassimo = 400_000;
        p.Condizioni =
        [
            Tasso("TAN1", "TAN fisso agevolato giovani", 2.80m),
            .. CostiMutuo(istruttoria: 0).Where(c => c.Categoria != CategoriaCondizione.SpesaIstruttoria)
        ];
        return p;
    }

    private static Prodotto MutuoTerritorio()
    {
        var p = Mutuo("MUT-LOM", "Mutuo Territorio Lombardia", "Offerta riservata agli immobili situati nelle province lombarde.",
            TipoTasso.Fisso, Finalita.AcquistoPrimaCasa, Finalita.Surroga);
        p.ProvinceAmmesse = ["BG", "BS", "CO", "CR", "LC", "LO", "MN", "MI", "MB", "PV", "SO", "VA"];
        p.LtvMassimo = 70;
        p.Condizioni =
        [
            Tasso("TAN1", "TAN fisso territorio", 2.75m),
            .. CostiMutuo()
        ];
        return p;
    }

    private static List<Condizione> CostiPrestito(decimal istruttoria, decimal incasso) =>
    [
        .. istruttoria > 0
            ? new List<Condizione> { new() { Codice = "IST", Categoria = CategoriaCondizione.SpesaIstruttoria, Descrizione = "Spese di istruttoria", TipoValore = TipoValore.Importo, Valore = istruttoria, Periodicita = Periodicita.UnaTantum, DataInizioValidita = Inizio } }
            : [],
        new() { Codice = "INC", Categoria = CategoriaCondizione.CommissioneIncassoRata, Descrizione = "Commissione incasso rata", TipoValore = TipoValore.Importo, Valore = incasso, Periodicita = Periodicita.Mensile, DataInizioValidita = Inizio },
        new() { Codice = "BOL", Categoria = CategoriaCondizione.ImpostaBollo, Descrizione = "Imposta di bollo sul contratto", TipoValore = TipoValore.Importo, Valore = 16, Periodicita = Periodicita.UnaTantum, DataInizioValidita = Inizio }
    ];

    private static Prodotto PrestitoLiquidita() => new()
    {
        Codice = "PRE-LIQ",
        Nome = "Prestito Personale Liquidità",
        Descrizione = "Prestito senza vincolo di destinazione per le esigenze personali e familiari.",
        Tipologia = TipologiaProdotto.PrestitoPersonale,
        TipoTasso = TipoTasso.Fisso,
        FinalitaAmmesse = [Finalita.Liquidita, Finalita.Arredamento, Finalita.Studi, Finalita.Altro],
        ImportoMinimo = 3_000,
        ImportoMassimo = 50_000,
        DurataMinimaMesi = 12,
        DurataMassimaMesi = 120,
        EtaMinima = 18,
        EtaMassimaFineFinanziamento = 75,
        DataInizioValidita = Inizio,
        Condizioni =
        [
            Tasso("TAN1", "TAN fisso importi fino a 15.000 €", 7.90m, importoA: 15_000),
            Tasso("TAN2", "TAN fisso importi oltre 15.000 €", 6.90m, importoDa: 15_000.01m),
            .. CostiPrestito(istruttoria: 150, incasso: 1.5m)
        ]
    };

    private static Prodotto PrestitoAuto() => new()
    {
        Codice = "PRE-AUTO",
        Nome = "Prestito Auto",
        Descrizione = "Finanziamento per l'acquisto di auto e moto, nuove o usate.",
        Tipologia = TipologiaProdotto.PrestitoFinalizzato,
        TipoTasso = TipoTasso.Fisso,
        FinalitaAmmesse = [Finalita.Auto],
        ImportoMinimo = 5_000,
        ImportoMassimo = 60_000,
        DurataMinimaMesi = 12,
        DurataMassimaMesi = 96,
        EtaMinima = 18,
        EtaMassimaFineFinanziamento = 75,
        DataInizioValidita = Inizio,
        Condizioni =
        [
            Tasso("TAN1", "TAN fisso", 5.90m),
            .. CostiPrestito(istruttoria: 100, incasso: 1m)
        ]
    };

    private static Prodotto PrestitoGreen() => new()
    {
        Codice = "PRE-GREEN",
        Nome = "Prestito Ristrutturazione Green",
        Descrizione = "Per lavori di ristrutturazione ed efficientamento energetico della casa.",
        Tipologia = TipologiaProdotto.PrestitoFinalizzato,
        TipoTasso = TipoTasso.Fisso,
        FinalitaAmmesse = [Finalita.Ristrutturazione],
        ImportoMinimo = 3_000,
        ImportoMassimo = 75_000,
        DurataMinimaMesi = 12,
        DurataMassimaMesi = 120,
        EtaMinima = 18,
        EtaMassimaFineFinanziamento = 75,
        DataInizioValidita = Inizio,
        Condizioni =
        [
            Tasso("TAN1", "TAN fisso green", 5.50m),
            .. CostiPrestito(istruttoria: 0, incasso: 1m)
        ]
    };
}
