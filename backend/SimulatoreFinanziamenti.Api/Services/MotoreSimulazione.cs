using SimulatoreFinanziamenti.Api.Domain;
using SimulatoreFinanziamenti.Api.Dtos;
using static SimulatoreFinanziamenti.Api.Services.CalcoloFinanziario;

namespace SimulatoreFinanziamenti.Api.Services;

/// <summary>
/// Seleziona i prodotti adatti alla richiesta del cliente, applica le condizioni del
/// condizioniere e calcola rata, TAEG e piano di ammortamento per ciascuno.
/// </summary>
public class MotoreSimulazione
{
    public RisultatoSimulazione Simula(RichiestaSimulazione richiesta, IEnumerable<Prodotto> prodotti, IEnumerable<IndiceRiferimento> indici)
    {
        var decorrenza = richiesta.DataDecorrenza ?? DateOnly.FromDateTime(DateTime.Today);
        var ltv = richiesta.ValoreImmobile is > 0 ? Arrotonda(richiesta.Importo / richiesta.ValoreImmobile.Value * 100) : (decimal?)null;
        var valoriIndici = indici.ToDictionary(i => i.Codice, i => i.Valore, StringComparer.OrdinalIgnoreCase);

        var offerte = new List<Offerta>();
        var nonDisponibili = new List<ProdottoNonDisponibile>();

        foreach (var prodotto in prodotti.Where(p => p.Attivo).OrderBy(p => p.Codice))
        {
            // Sono esclusi in silenzio i prodotti di altra tipologia o finalità: non interessano al cliente.
            if (richiesta.Tipologia is { } t && prodotto.Tipologia != t) continue;
            if (!prodotto.FinalitaAmmesse.Contains(richiesta.Finalita)) continue;

            var motivi = VerificaRequisiti(prodotto, richiesta, ltv, decorrenza);
            var condizioni = prodotto.Condizioni.Where(c => Applicabile(c, richiesta, ltv, decorrenza)).OrderBy(c => c.Codice).ToList();
            var tasso = condizioni.FirstOrDefault(c => c.Categoria == CategoriaCondizione.Tasso);
            decimal? valoreIndice = null;

            if (motivi.Count == 0 && tasso is null)
                motivi.Add("Nessun tasso previsto per l'importo, la durata o l'LTV richiesti");
            if (motivi.Count == 0 && prodotto.TipoTasso == TipoTasso.Variabile)
            {
                if (prodotto.IndiceRiferimento is { } codice && valoriIndici.TryGetValue(codice, out var v)) valoreIndice = v;
                else motivi.Add("Indice di riferimento del tasso variabile non configurato");
            }

            if (motivi.Count > 0)
            {
                nonDisponibili.Add(new ProdottoNonDisponibile(prodotto.Id, prodotto.Nome, motivi));
                continue;
            }

            offerte.Add(CalcolaOfferta(prodotto, richiesta, tasso!, valoreIndice, condizioni.Where(c => c.Categoria != CategoriaCondizione.Tasso).ToList(), decorrenza));
        }

        return new RisultatoSimulazione
        {
            Ltv = ltv,
            Offerte = [.. offerte.OrderBy(o => o.Taeg).ThenBy(o => o.Rata)],
            NonDisponibili = nonDisponibili
        };
    }

    private static List<string> VerificaRequisiti(Prodotto p, RichiestaSimulazione r, decimal? ltv, DateOnly decorrenza)
    {
        var motivi = new List<string>();
        if (decorrenza < p.DataInizioValidita || (p.DataFineValidita is { } fine && decorrenza > fine))
            motivi.Add("Prodotto non in vigore alla data richiesta");
        if (r.Importo < p.ImportoMinimo || r.Importo > p.ImportoMassimo)
            motivi.Add($"Importo ammesso da {p.ImportoMinimo:N0} € a {p.ImportoMassimo:N0} €");
        if (r.DurataMesi < p.DurataMinimaMesi || r.DurataMesi > p.DurataMassimaMesi)
            motivi.Add($"Durata ammessa da {p.DurataMinimaMesi} a {p.DurataMassimaMesi} mesi");
        if (r.EtaRichiedente < p.EtaMinima)
            motivi.Add($"Età minima {p.EtaMinima} anni");
        if (p.EtaMassimaRichiedente is { } etaMax && r.EtaRichiedente > etaMax)
            motivi.Add($"Riservato a chi ha al massimo {etaMax} anni");
        if (p.EtaMassimaFineFinanziamento is { } etaFine && r.EtaRichiedente + (r.DurataMesi + 11) / 12 > etaFine)
            motivi.Add($"Alla scadenza il richiedente non deve superare {etaFine} anni");
        if (p.ProvinceAmmesse.Count > 0 && (r.Provincia is null || !p.ProvinceAmmesse.Contains(r.Provincia, StringComparer.OrdinalIgnoreCase)))
            motivi.Add("Non disponibile nella provincia indicata");
        // Il valore dell'immobile si chiede solo per le finalità legate a un immobile: per le altre
        // (es. un mutuo di liquidità) il simulatore non lo raccoglie e l'LTV non si verifica.
        if (ltv is null)
        {
            if (p.Tipologia == TipologiaProdotto.Mutuo && r.Finalita.RichiedeImmobile())
                motivi.Add("Indicare il valore dell'immobile");
        }
        else if (p.LtvMassimo is { } ltvMax && ltv > ltvMax)
            motivi.Add($"Finanziabile al massimo il {ltvMax:0.##}% del valore dell'immobile");
        return motivi;
    }

    public static bool Applicabile(Condizione c, RichiestaSimulazione r, decimal? ltv, DateOnly decorrenza) =>
        decorrenza >= c.DataInizioValidita
        && (c.DataFineValidita is null || decorrenza <= c.DataFineValidita)
        && (c.Finalita is null || c.Finalita == r.Finalita)
        && (c.FasciaImportoDa is null || r.Importo >= c.FasciaImportoDa)
        && (c.FasciaImportoA is null || r.Importo <= c.FasciaImportoA)
        && (c.FasciaDurataDaMesi is null || r.DurataMesi >= c.FasciaDurataDaMesi)
        && (c.FasciaDurataAMesi is null || r.DurataMesi <= c.FasciaDurataAMesi)
        && (c.FasciaLtvDa is null || (ltv is not null && ltv >= c.FasciaLtvDa))
        && (c.FasciaLtvA is null || (ltv is not null && ltv <= c.FasciaLtvA));

    /// <summary>Importo di una singola applicazione della condizione (percentuali sull'importo finanziato).</summary>
    public static decimal ImportoCondizione(Condizione c, decimal importoFinanziato)
    {
        if (c.TipoValore == TipoValore.Importo) return Arrotonda(c.Valore);
        var valore = importoFinanziato * c.Valore / 100m;
        if (c.ImportoMinimo is { } min) valore = Math.Max(valore, min);
        if (c.ImportoMassimo is { } max) valore = Math.Min(valore, max);
        return Arrotonda(valore);
    }

    private static Offerta CalcolaOfferta(Prodotto p, RichiestaSimulazione r, Condizione tasso, decimal? valoreIndice, List<Condizione> costi, DateOnly decorrenza)
    {
        var tan = Math.Max(0, tasso.Valore + (valoreIndice ?? 0));
        var n = r.DurataMesi;
        var anni = (n + 11) / 12;

        // Importo per ogni applicazione e numero di applicazioni nell'intera durata.
        var voci = costi.Select(c =>
        {
            var importo = ImportoCondizione(c, r.Importo);
            var volte = c.Periodicita switch { Periodicita.Mensile => n, Periodicita.Annuale => anni, _ => 1 };
            return (Condizione: c, Importo: importo, Totale: importo * volte);
        }).ToList();

        decimal SpesePeriodiche(int rata, bool soloTaeg) => voci
            .Where(v => !soloTaeg || v.Condizione.InclusaNelTaeg)
            .Sum(v => v.Condizione.Periodicita switch
            {
                Periodicita.Mensile => v.Importo,
                // Le spese annuali si pagano con la prima rata di ogni anno.
                Periodicita.Annuale when (rata - 1) % 12 == 0 => v.Importo,
                _ => 0m
            });

        var piano = PianoAmmortamento(r.Importo, tan, n, decorrenza, rata => SpesePeriodiche(rata, false));
        var costiIniziali = voci.Where(v => v.Condizione.Periodicita == Periodicita.UnaTantum && v.Condizione.InclusaNelTaeg).Sum(v => v.Importo);
        var pagamentiTaeg = piano.Select(rp => rp.Rata + SpesePeriodiche(rp.Numero, true)).ToList();

        var totaleInteressi = piano.Sum(rp => rp.QuotaInteressi);
        var totaleSpese = voci.Sum(v => v.Totale);

        return new Offerta
        {
            ProdottoId = p.Id,
            Codice = p.Codice,
            Nome = p.Nome,
            Descrizione = p.Descrizione,
            Tipologia = p.Tipologia,
            TipoTasso = p.TipoTasso,
            IndiceRiferimento = p.TipoTasso == TipoTasso.Variabile ? p.IndiceRiferimento : null,
            ValoreIndice = valoreIndice,
            Spread = p.TipoTasso == TipoTasso.Variabile ? tasso.Valore : null,
            Tan = tan,
            Taeg = Taeg(r.Importo - costiIniziali, pagamentiTaeg),
            Rata = piano[0].Rata,
            Importo = r.Importo,
            DurataMesi = n,
            TotaleInteressi = totaleInteressi,
            TotaleSpese = totaleSpese,
            ImportoTotaleDovuto = r.Importo + totaleInteressi + totaleSpese,
            Costi = [.. voci.Select(v => new VoceCosto(v.Condizione.Codice, v.Condizione.Categoria, v.Condizione.Descrizione, v.Condizione.Periodicita, v.Importo, v.Totale, v.Condizione.InclusaNelTaeg))],
            PianoAmmortamento = piano
        };
    }
}
