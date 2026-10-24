using System.ComponentModel.DataAnnotations;

namespace SimulatoreFinanziamenti.Api.Domain;

/// <summary>
/// Prodotto della "fabbrica prodotti": definisce a chi e per cosa il finanziamento
/// può essere proposto. Le condizioni economiche sono nel condizioniere (<see cref="Condizione"/>).
/// </summary>
public class Prodotto
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Codice { get; set; } = "";

    [Required, MaxLength(100)]
    public string Nome { get; set; } = "";

    [MaxLength(1000)]
    public string? Descrizione { get; set; }

    public TipologiaProdotto Tipologia { get; set; }

    public TipoTasso TipoTasso { get; set; }

    /// <summary>Codice dell'indice di riferimento (es. EUR3M), solo per i prodotti a tasso variabile.</summary>
    [MaxLength(20)]
    public string? IndiceRiferimento { get; set; }

    public List<Finalita> FinalitaAmmesse { get; set; } = [];

    [Range(0, double.MaxValue)]
    public decimal ImportoMinimo { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ImportoMassimo { get; set; }

    [Range(1, 600)]
    public int DurataMinimaMesi { get; set; }

    [Range(1, 600)]
    public int DurataMassimaMesi { get; set; }

    [Range(18, 120)]
    public int EtaMinima { get; set; } = 18;

    /// <summary>Età massima del richiedente alla data della richiesta (es. 35 per i prodotti under 36).</summary>
    [Range(18, 120)]
    public int? EtaMassimaRichiedente { get; set; }

    /// <summary>Età massima del richiedente alla scadenza dell'ultima rata.</summary>
    [Range(18, 120)]
    public int? EtaMassimaFineFinanziamento { get; set; }

    /// <summary>Loan to value massimo in percentuale (solo mutui).</summary>
    [Range(0, 100)]
    public decimal? LtvMassimo { get; set; }

    /// <summary>Sigle delle province in cui il prodotto è disponibile. Vuoto = tutte.</summary>
    public List<string> ProvinceAmmesse { get; set; } = [];

    public bool Attivo { get; set; } = true;

    public DateOnly DataInizioValidita { get; set; }

    public DateOnly? DataFineValidita { get; set; }

    public List<Condizione> Condizioni { get; set; } = [];
}
