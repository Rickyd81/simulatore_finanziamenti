using SimulatoreFinanziamenti.Api.Dtos;

namespace SimulatoreFinanziamenti.Api.Services;

/// <summary>Formule finanziarie: ammortamento alla francese (rata costante) e calcolo del TAEG.</summary>
public static class CalcoloFinanziario
{
    public static decimal Arrotonda(decimal valore) => Math.Round(valore, 2, MidpointRounding.AwayFromZero);

    /// <summary>Rata costante mensile per un TAN annuo espresso in percentuale.</summary>
    public static decimal RataCostante(decimal importo, decimal tanPercentuale, int durataMesi)
    {
        if (durataMesi <= 0) throw new ArgumentOutOfRangeException(nameof(durataMesi));
        var i = (double)tanPercentuale / 100d / 12d;
        if (i == 0) return Arrotonda(importo / durataMesi);
        var rata = (double)importo * i / (1 - Math.Pow(1 + i, -durataMesi));
        return Arrotonda((decimal)rata);
    }

    /// <summary>
    /// Piano di ammortamento alla francese. <paramref name="spesePerRata"/> restituisce le spese
    /// periodiche dovute con la rata n (base 1).
    /// </summary>
    public static List<RataPiano> PianoAmmortamento(decimal importo, decimal tanPercentuale, int durataMesi, DateOnly decorrenza, Func<int, decimal> spesePerRata)
    {
        var rata = RataCostante(importo, tanPercentuale, durataMesi);
        var tassoMensile = tanPercentuale / 100m / 12m;
        var residuo = importo;
        var piano = new List<RataPiano>(durataMesi);

        for (var n = 1; n <= durataMesi; n++)
        {
            var interessi = Arrotonda(residuo * tassoMensile);
            var capitale = n == durataMesi ? residuo : Math.Min(rata - interessi, residuo);
            residuo -= capitale;
            piano.Add(new RataPiano(n, decorrenza.AddMonths(n), capitale + interessi, capitale, interessi, spesePerRata(n), residuo));
        }

        return piano;
    }

    /// <summary>
    /// TAEG in percentuale: tasso annuo che eguaglia l'importo effettivamente erogato
    /// (importo meno costi iniziali) al valore attuale dei pagamenti mensili.
    /// </summary>
    public static decimal Taeg(decimal importoNetto, IReadOnlyList<decimal> pagamentiMensili)
    {
        var netto = (double)importoNetto;
        var pagamenti = pagamentiMensili.Select(p => (double)p).ToArray();

        double ValoreAttuale(double i)
        {
            var va = 0d;
            var sconto = 1d;
            foreach (var p in pagamenti)
            {
                sconto /= 1 + i;
                va += p * sconto;
            }
            return va - netto;
        }

        // Il valore attuale decresce al crescere del tasso: ricerca per bisezione.
        double basso = -0.99, alto = 1;
        if (ValoreAttuale(basso) < 0 || ValoreAttuale(alto) > 0) return 0;
        for (var k = 0; k < 200; k++)
        {
            var medio = (basso + alto) / 2;
            if (ValoreAttuale(medio) > 0) basso = medio; else alto = medio;
        }

        var annuo = Math.Pow(1 + (basso + alto) / 2, 12) - 1;
        return Math.Round((decimal)(annuo * 100), 2, MidpointRounding.AwayFromZero);
    }
}
