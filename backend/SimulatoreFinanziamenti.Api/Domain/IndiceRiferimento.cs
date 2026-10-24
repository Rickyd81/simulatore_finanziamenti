using System.ComponentModel.DataAnnotations;

namespace SimulatoreFinanziamenti.Api.Domain;

/// <summary>Parametro di mercato per i tassi variabili (es. Euribor 3 mesi).</summary>
public class IndiceRiferimento
{
    [Key, MaxLength(20)]
    public string Codice { get; set; } = "";

    [Required, MaxLength(100)]
    public string Descrizione { get; set; } = "";

    /// <summary>Valore in percentuale.</summary>
    public decimal Valore { get; set; }

    public DateOnly DataRilevazione { get; set; }
}
