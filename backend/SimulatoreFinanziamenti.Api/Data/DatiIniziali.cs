using SimulatoreFinanziamenti.Api.Domain;

namespace SimulatoreFinanziamenti.Api.Data;

/// <summary>Catalogo di esempio caricato al primo avvio, quando il database è vuoto.</summary>
public static class DatiIniziali
{
    private static readonly DateOnly Inizio = new(2026, 1, 1);

    /// <summary>
    /// Carica il catalogo di esempio su un database vuoto. Con <paramref name="caricaAnagrafica"/> carica
    /// l'anagrafica condizioni (tabella appena creata), con <paramref name="caricaPricing"/> completa
    /// l'anagrafica pricing (spread o tipo di tasso appena introdotti).
    /// </summary>
    public static void Carica(SimulatoreDbContext db, bool caricaAnagrafica = false, bool caricaPricing = false)
    {
        // L'anagrafica di esempio si carica solo quando la tabella è appena stata creata,
        // così chi la svuota volutamente non se la ritrova al riavvio.
        if (caricaAnagrafica && !db.CondizioniAnagrafica.Any())
        {
            db.CondizioniAnagrafica.AddRange(AnagraficaCondizioni());
            db.SaveChanges();
        }

        // Quando spread o tipo di tasso entrano nell'anagrafica pricing (database delle versioni precedenti)
        // si aggiungono i pricing di esempio mancanti e si completano quelli esistenti con lo stesso codice.
        // I pricing senza tipo di tasso erano gli indici dei prodotti variabili: restano variabili.
        if (caricaPricing)
        {
            var esempi = PricingEsempio();
            foreach (var esempio in esempi)
            {
                var esistente = db.Pricing.Find(esempio.Codice);
                if (esistente is null) db.Pricing.Add(esempio);
                else esistente.Spread ??= esempio.Spread;
            }
            foreach (var pricing in db.Pricing.Where(p => p.TipoTasso == null).ToList())
                pricing.TipoTasso = TipoTasso.Variabile;
            db.SaveChanges();
        }

        PricingNelleCondizioni(db);

        if (db.Prodotti.Any()) return;

        if (!db.Pricing.Any()) db.Pricing.AddRange(PricingEsempio());

        db.Prodotti.AddRange(
            MutuoFisso(), MutuoVariabile(), MutuoGiovani(), MutuoTerritorio(),
            PrestitoLiquidita(), PrestitoAuto(), PrestitoGreen());

        db.SaveChanges();
    }

    private static readonly DateOnly Rilevazione = new(2026, 10, 1);

    /// <summary>Pricing assegnato alle condizioni di tasso delle versioni precedenti che non ne avevano uno.</summary>
    public const string CodicePricingPrecedenti = "TF-PREC";

    /// <summary>
    /// Porta il pricing sulle condizioni di tasso nei database delle versioni precedenti, lasciando invariato il TAN.
    /// Prima il pricing (e lo spread) stava sul prodotto e le condizioni di tasso ne indicavano lo spread; ancora prima
    /// i prodotti fissi avevano il TAN direttamente nelle condizioni. Si ripete a ogni avvio: tocca solo ciò che manca.
    /// </summary>
    private static void PricingNelleCondizioni(SimulatoreDbContext db)
    {
        var valori = db.Pricing.AsEnumerable().ToDictionary(p => p.Codice, p => p.Valore, StringComparer.OrdinalIgnoreCase);
        var modificato = false;

        // 1. Pricing sul prodotto: passa alle sue condizioni di tasso, con il tasso di riferimento di oggi.
        foreach (var prodotto in db.Prodotti.Where(p => p.CodicePricing != null).ToList())
        {
            var codice = prodotto.CodicePricing!;
            var tasso = valori.GetValueOrDefault(codice);
            var condizioni = db.Condizioni.Where(c => c.ProdottoId == prodotto.Id && c.Categoria == CategoriaCondizione.Tasso).ToList();
            foreach (var c in condizioni.Where(c => c.CodicePricing == null))
            {
                c.CodicePricing = codice;
                c.TassoRiferimento = tasso;
            }
            // Senza condizioni di tasso valeva lo spread del prodotto: diventa una condizione di tasso.
            if (condizioni.Count == 0 && prodotto.Spread is { } spread)
            {
                var codici = db.Condizioni.Where(c => c.ProdottoId == prodotto.Id).Select(c => c.Codice).ToHashSet();
                var nuovoCodice = Enumerable.Range(1, 99).Select(i => $"TAN{i}").First(c => !codici.Contains(c));
                db.Condizioni.Add(new Condizione
                {
                    ProdottoId = prodotto.Id,
                    Codice = nuovoCodice,
                    Categoria = CategoriaCondizione.Tasso,
                    Descrizione = $"Tasso {codice} + spread",
                    TipoValore = TipoValore.Percentuale,
                    Valore = spread,
                    CodicePricing = codice,
                    TassoRiferimento = tasso,
                    Periodicita = Periodicita.Annuale,
                    DataInizioValidita = prodotto.DataInizioValidita
                });
            }
            prodotto.CodicePricing = null;
            prodotto.Spread = null;
            modificato = true;
        }
        // Si salva subito: il passo successivo interroga il database sulle condizioni ancora senza pricing.
        if (modificato) db.SaveChanges();

        // 2. Condizioni di tasso ancora senza pricing: il valore è il TAN. Ricevono il pricing a zero (TAN = 0 + spread).
        var senzaPricing = db.Condizioni.Where(c => c.Categoria == CategoriaCondizione.Tasso && c.CodicePricing == null).ToList();
        var anagraficaSenzaPricing = db.CondizioniAnagrafica.Where(c => c.Categoria == CategoriaCondizione.Tasso && c.CodicePricing == null).ToList();
        if (senzaPricing.Count > 0 || anagraficaSenzaPricing.Count > 0)
        {
            if (!valori.ContainsKey(CodicePricingPrecedenti))
                db.Pricing.Add(Pricing(CodicePricingPrecedenti, "Tasso fisso delle versioni precedenti: TAN nello spread", TipoTasso.Fisso, 0, 0));
            foreach (var c in senzaPricing.Cast<DatiCondizione>().Concat(anagraficaSenzaPricing))
            {
                c.CodicePricing = CodicePricingPrecedenti;
                c.TassoRiferimento = 0;
            }
            db.SaveChanges();
        }
    }

    private static Pricing Pricing(string codice, string descrizione, TipoTasso tipo, decimal valore, decimal spread) =>
        new() { Codice = codice, Descrizione = descrizione, TipoTasso = tipo, Valore = valore, Spread = spread, DataRilevazione = Rilevazione };

    private static List<Pricing> PricingEsempio() =>
    [
        // Tasso fisso: tasso base (IRS della durata o tasso base dei prestiti) + spread.
        Pricing("IRS10", "IRS 10 anni", TipoTasso.Fisso, 2.30m, 0.90m),
        Pricing("IRS20", "IRS 20 anni", TipoTasso.Fisso, 2.45m, 0.95m),
        Pricing("IRS20-GIO", "IRS 20 anni, spread agevolato giovani", TipoTasso.Fisso, 2.45m, 0.35m),
        Pricing("IRS30", "IRS 30 anni", TipoTasso.Fisso, 2.50m, 0.90m),
        Pricing("TBP", "Tasso base prestiti personali", TipoTasso.Fisso, 3.50m, 3.40m),
        Pricing("TBP-GREEN", "Tasso base prestiti, spread green", TipoTasso.Fisso, 3.50m, 2.00m),
        // Tasso variabile: indice di mercato + spread.
        Pricing("EUR1M", "Euribor 1 mese", TipoTasso.Variabile, 1.95m, 1.40m),
        Pricing("EUR3M", "Euribor 3 mesi", TipoTasso.Variabile, 2.05m, 1.20m),
        Pricing("EUR6M", "Euribor 6 mesi", TipoTasso.Variabile, 2.12m, 1.10m),
        Pricing("EUR3M-GIO", "Euribor 3 mesi, spread agevolato giovani", TipoTasso.Variabile, 2.05m, 0.85m),
        Pricing("BCE", "Tasso BCE operazioni di rifinanziamento principali", TipoTasso.Variabile, 2.15m, 3.50m)
    ];


    private static IEnumerable<CondizioneAnagrafica> AnagraficaCondizioni() =>
    [
        .. CostiMutuo().Select(c => InAnagrafica(c, "Costi mutuo")),
        .. CostiPrestito(istruttoria: 150, incasso: 1.5m).Select(c => InAnagrafica(c, "Costi prestito", c.Codice == "BOL" ? "BOL" : c.Codice + "-PRE")),
        .. MutuoFisso().Condizioni.Where(c => c.Categoria == CategoriaCondizione.Tasso).Select(c => InAnagrafica(c, "Tassi mutuo fisso per durata e LTV", "MF-" + c.Codice))
    ];

    private static CondizioneAnagrafica InAnagrafica(Condizione c, string gruppo, string? codice = null)
    {
        var a = new CondizioneAnagrafica { Gruppo = gruppo };
        a.CopiaDati(c);
        if (codice is not null) a.Codice = codice;
        return a;
    }

    private static Prodotto Mutuo(string codice, string nome, string descrizione, TipoTasso tipoTasso, params Finalita[] finalita)
    {
        var p = new Prodotto
        {
            Codice = codice,
            Nome = nome,
            Descrizione = descrizione,
            Tipologia = TipologiaProdotto.Mutuo,
            TipoTasso = tipoTasso,
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
        return p;
    }

    private static List<Condizione> CostiMutuo(decimal istruttoria = 1m) =>
    [
        new() { Codice = "IST", Categoria = CategoriaCondizione.SpesaIstruttoria, Descrizione = "Spese di istruttoria", TipoValore = TipoValore.Percentuale, Valore = istruttoria, ImportoMinimo = istruttoria > 0 ? 500 : null, ImportoMassimo = istruttoria > 0 ? 1_500 : null, Periodicita = Periodicita.UnaTantum, DataInizioValidita = Inizio },
        new() { Codice = "PER", Categoria = CategoriaCondizione.SpesaPerizia, Descrizione = "Perizia tecnica dell'immobile", TipoValore = TipoValore.Importo, Valore = 300, Periodicita = Periodicita.UnaTantum, DataInizioValidita = Inizio },
        new() { Codice = "INC", Categoria = CategoriaCondizione.CommissioneIncassoRata, Descrizione = "Commissione incasso rata (addebito in conto)", TipoValore = TipoValore.Importo, Valore = 2, Periodicita = Periodicita.Mensile, DataInizioValidita = Inizio },
        new() { Codice = "ASS", Categoria = CategoriaCondizione.Assicurazione, Descrizione = "Polizza incendio e scoppio obbligatoria", TipoValore = TipoValore.Importo, Valore = 120, Periodicita = Periodicita.Annuale, DataInizioValidita = Inizio },
        new() { Codice = "IMP1", Categoria = CategoriaCondizione.ImpostaSostitutiva, Descrizione = "Imposta sostitutiva prima casa (0,25%)", TipoValore = TipoValore.Percentuale, Valore = 0.25m, Periodicita = Periodicita.UnaTantum, Finalita = Finalita.AcquistoPrimaCasa, DataInizioValidita = Inizio },
        new() { Codice = "IMP2", Categoria = CategoriaCondizione.ImpostaSostitutiva, Descrizione = "Imposta sostitutiva seconda casa (2%)", TipoValore = TipoValore.Percentuale, Valore = 2m, Periodicita = Periodicita.UnaTantum, Finalita = Finalita.AcquistoSecondaCasa, DataInizioValidita = Inizio }
    ];

    /// <summary>Condizione di tasso: richiama il pricing indicato con il suo tasso di riferimento e lo spread dato.</summary>
    private static Condizione Tasso(string codice, string descrizione, string pricing, decimal spread, int? durataDa = null, int? durataA = null, decimal? ltvDa = null, decimal? ltvA = null, decimal? importoDa = null, decimal? importoA = null) => new()
    {
        Codice = codice,
        Categoria = CategoriaCondizione.Tasso,
        Descrizione = descrizione,
        TipoValore = TipoValore.Percentuale,
        CodicePricing = pricing,
        TassoRiferimento = PricingEsempio().Single(p => p.Codice == pricing).Valore,
        Valore = spread,
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
            // IRS (2,30% a 10 anni, 2,45% a 20 anni) + spread per durata e LTV: TAN dal 2,95% al 3,40%.
            Tasso("TAN1", "TAN fisso fino a 20 anni, LTV fino al 60%", "IRS10", 0.65m, durataA: 240, ltvA: 60),
            Tasso("TAN2", "TAN fisso fino a 20 anni, LTV oltre il 60%", "IRS10", 0.85m, durataA: 240, ltvDa: 60),
            Tasso("TAN3", "TAN fisso oltre 20 anni, LTV fino al 60%", "IRS20", 0.75m, durataDa: 241, ltvA: 60),
            Tasso("TAN4", "TAN fisso oltre 20 anni, LTV oltre il 60%", "IRS20", 0.95m, durataDa: 241, ltvDa: 60),
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
            Tasso("TAN1", "Euribor 3M + spread, LTV fino al 60%", "EUR3M", 0.95m, ltvA: 60),
            Tasso("TAN2", "Euribor 3M + spread, LTV oltre il 60%", "EUR3M", 1.20m, ltvDa: 60),
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
            Tasso("TAN1", "TAN fisso agevolato giovani", "IRS20-GIO", 0.35m),
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
            // Spread ridotto rispetto a quello proposto dal pricing IRS20 (0,95%).
            Tasso("TAN1", "TAN fisso territorio", "IRS20", 0.30m),
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
            Tasso("TAN1", "TAN fisso importi fino a 15.000 €", "TBP", 4.40m, importoA: 15_000),
            Tasso("TAN2", "TAN fisso importi oltre 15.000 €", "TBP", 3.40m, importoDa: 15_000.01m),
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
            Tasso("TAN1", "TAN fisso", "TBP", 2.40m),
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
            Tasso("TAN1", "TAN fisso green", "TBP-GREEN", 2.00m),
            .. CostiPrestito(istruttoria: 0, incasso: 1m)
        ]
    };
}
