using System.ComponentModel.DataAnnotations;

namespace SimulatoreFinanziamenti.Api.Domain;

/// <summary>
/// Pricing dell'anagrafica pricing: la coppia tasso di riferimento + spread da cui nasce il TAN dei
/// prodotti, a tasso fisso (es. IRS 20 anni) o variabile (es. Euribor 3 mesi). Il prodotto può richiamare
/// solo pricing del suo tipo di tasso e ne importa lo spread come copia.
/// </summary>
public class Pricing
{
    [Key, MaxLength(20)]
    public string Codice { get; set; } = "";

    [Required, MaxLength(100)]
    public string Descrizione { get; set; } = "";

    /// <summary>
    /// Tipo di tasso dei prodotti che possono richiamare il pricing. Nullable solo perché i pricing
    /// delle versioni precedenti non l'avevano (all'aggiornamento diventano variabili): l'API lo richiede.
    /// </summary>
    public TipoTasso? TipoTasso { get; set; }

    /// <summary>
    /// Tasso di riferimento in percentuale: l'indice per il variabile (es. Euribor), il tasso base per il
    /// fisso (es. IRS della durata). Per il variabile il valore aggiornato vale per tutti i prodotti.
    /// </summary>
    public decimal Valore { get; set; }

    public DateOnly DataRilevazione { get; set; }

    /// <summary>
    /// Spread in percentuale da sommare all'indice. Nullable solo perché i pricing creati
    /// dalle versioni precedenti (gli "indici di riferimento") non l'avevano: l'API lo richiede.
    /// </summary>
    public decimal? Spread { get; set; }
}
