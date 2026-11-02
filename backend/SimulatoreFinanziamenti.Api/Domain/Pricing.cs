using System.ComponentModel.DataAnnotations;

namespace SimulatoreFinanziamenti.Api.Domain;

/// <summary>
/// Pricing dell'anagrafica pricing: l'indice di mercato per i tassi variabili (es. Euribor 3 mesi)
/// con lo spread proposto. Il prodotto che richiama un pricing ne importa lo spread come copia.
/// </summary>
public class Pricing
{
    [Key, MaxLength(20)]
    public string Codice { get; set; } = "";

    [Required, MaxLength(100)]
    public string Descrizione { get; set; } = "";

    /// <summary>Valore dell'indice in percentuale.</summary>
    public decimal Valore { get; set; }

    public DateOnly DataRilevazione { get; set; }

    /// <summary>
    /// Spread in percentuale da sommare all'indice. Nullable solo perché i pricing creati
    /// dalle versioni precedenti (gli "indici di riferimento") non l'avevano: l'API lo richiede.
    /// </summary>
    public decimal? Spread { get; set; }
}
