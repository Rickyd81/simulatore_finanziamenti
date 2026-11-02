using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SimulatoreFinanziamenti.Api.Domain;

/// <summary>
/// Dati economici comuni alle condizioni dei prodotti e a quelle dell'anagrafica condizioni.
/// Le fasce (importo, durata, LTV, finalità) sono facoltative: una fascia non valorizzata
/// vale per qualsiasi valore.
/// </summary>
public abstract class DatiCondizione
{
    public int Id { get; set; }

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

    /// <summary>Copia i dati economici (codice compreso) da un'altra condizione.</summary>
    public void CopiaDati(DatiCondizione origine)
    {
        Codice = origine.Codice;
        Categoria = origine.Categoria;
        Descrizione = origine.Descrizione;
        TipoValore = origine.TipoValore;
        Valore = origine.Valore;
        ImportoMinimo = origine.ImportoMinimo;
        ImportoMassimo = origine.ImportoMassimo;
        Periodicita = origine.Periodicita;
        InclusaNelTaeg = origine.InclusaNelTaeg;
        FasciaImportoDa = origine.FasciaImportoDa;
        FasciaImportoA = origine.FasciaImportoA;
        FasciaDurataDaMesi = origine.FasciaDurataDaMesi;
        FasciaDurataAMesi = origine.FasciaDurataAMesi;
        FasciaLtvDa = origine.FasciaLtvDa;
        FasciaLtvA = origine.FasciaLtvA;
        Finalita = origine.Finalita;
        DataInizioValidita = origine.DataInizioValidita;
        DataFineValidita = origine.DataFineValidita;
    }
}

/// <summary>Condizione economica del condizioniere di un prodotto.</summary>
public class Condizione : DatiCondizione
{
    public int ProdottoId { get; set; }

    [JsonIgnore]
    public Prodotto? Prodotto { get; set; }

    /// <summary>
    /// Codice della condizione dell'anagrafica da cui è stata importata, null se inserita a mano.
    /// È una copia: le modifiche fatte sul prodotto non toccano l'anagrafica e viceversa.
    /// </summary>
    [MaxLength(20)]
    public string? CodiceAnagrafica { get; set; }

    /// <summary>Copia i dati economici di una condizione dell'anagrafica.</summary>
    public static Condizione DaAnagrafica(CondizioneAnagrafica a, int prodottoId)
    {
        var condizione = new Condizione { ProdottoId = prodottoId, CodiceAnagrafica = a.Codice };
        condizione.CopiaDati(a);
        return condizione;
    }
}

/// <summary>
/// Condizione pre-confezionata dell'anagrafica condizioni, da importare nel condizioniere
/// dei prodotti invece di inserirla ogni volta a mano.
/// </summary>
public class CondizioneAnagrafica : DatiCondizione
{
    /// <summary>Raggruppamento facoltativo (es. "Costi mutuo standard") per importare più condizioni insieme.</summary>
    [MaxLength(100)]
    public string? Gruppo { get; set; }
}
