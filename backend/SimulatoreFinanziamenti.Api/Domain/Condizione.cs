using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SimulatoreFinanziamenti.Api.Domain;

/// <summary>
/// Condizione economica del condizioniere. Le fasce (importo, durata, LTV, finalità)
/// sono facoltative: una fascia non valorizzata vale per qualsiasi valore.
/// </summary>
public class Condizione
{
    public int Id { get; set; }

    public int ProdottoId { get; set; }

    [JsonIgnore]
    public Prodotto? Prodotto { get; set; }

    [Required, MaxLength(20)]
    public string Codice { get; set; } = "";

    public CategoriaCondizione Categoria { get; set; }

    [Required, MaxLength(200)]
    public string Descrizione { get; set; } = "";

    public TipoValore TipoValore { get; set; }

    /// <summary>
    /// Valore della condizione. Per la categoria Tasso è il TAN annuo in percentuale,
    /// oppure lo spread da sommare all'indice di riferimento per i prodotti a tasso variabile.
    /// </summary>
    public decimal Valore { get; set; }

    /// <summary>Minimo applicato a una condizione percentuale (es. istruttoria 1% min 500 €).</summary>
    public decimal? ImportoMinimo { get; set; }

    /// <summary>Massimo applicato a una condizione percentuale.</summary>
    public decimal? ImportoMassimo { get; set; }

    public Periodicita Periodicita { get; set; }

    public bool InclusaNelTaeg { get; set; } = true;

    public decimal? FasciaImportoDa { get; set; }
    public decimal? FasciaImportoA { get; set; }
    public int? FasciaDurataDaMesi { get; set; }
    public int? FasciaDurataAMesi { get; set; }
    public decimal? FasciaLtvDa { get; set; }
    public decimal? FasciaLtvA { get; set; }

    /// <summary>Se valorizzata, la condizione si applica solo a questa finalità.</summary>
    public Finalita? Finalita { get; set; }

    public DateOnly DataInizioValidita { get; set; }

    public DateOnly? DataFineValidita { get; set; }
}
