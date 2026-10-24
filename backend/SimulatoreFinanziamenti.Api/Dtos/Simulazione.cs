using System.ComponentModel.DataAnnotations;
using SimulatoreFinanziamenti.Api.Domain;

namespace SimulatoreFinanziamenti.Api.Dtos;

public record RichiestaSimulazione
{
    public TipologiaProdotto? Tipologia { get; init; }

    [Required]
    public Finalita Finalita { get; init; }

    [Range(1, 100_000_000)]
    public decimal Importo { get; init; }

    [Range(1, 600)]
    public int DurataMesi { get; init; }

    /// <summary>Valore dell'immobile, obbligatorio per i mutui (serve a calcolare l'LTV).</summary>
    [Range(0, 1_000_000_000)]
    public decimal? ValoreImmobile { get; init; }

    /// <summary>Sigla della provincia dell'immobile o di residenza.</summary>
    [MaxLength(2)]
    public string? Provincia { get; init; }

    [Range(18, 120)]
    public int EtaRichiedente { get; init; }

    /// <summary>Data di decorrenza del piano. Se assente si usa la data odierna.</summary>
    public DateOnly? DataDecorrenza { get; init; }
}

public record VoceCosto(string Codice, CategoriaCondizione Categoria, string Descrizione, Periodicita Periodicita, decimal Importo, decimal ImportoTotale, bool InclusaNelTaeg);

public record RataPiano(int Numero, DateOnly DataScadenza, decimal Rata, decimal QuotaCapitale, decimal QuotaInteressi, decimal Spese, decimal DebitoResiduo);

public record Offerta
{
    public int ProdottoId { get; init; }
    public string Codice { get; init; } = "";
    public string Nome { get; init; } = "";
    public string? Descrizione { get; init; }
    public TipologiaProdotto Tipologia { get; init; }
    public TipoTasso TipoTasso { get; init; }
    public string? IndiceRiferimento { get; init; }
    public decimal? ValoreIndice { get; init; }
    public decimal? Spread { get; init; }
    public decimal Tan { get; init; }
    public decimal Taeg { get; init; }
    public decimal Rata { get; init; }
    public decimal Importo { get; init; }
    public int DurataMesi { get; init; }
    public decimal TotaleInteressi { get; init; }
    public decimal TotaleSpese { get; init; }
    public decimal ImportoTotaleDovuto { get; init; }
    public List<VoceCosto> Costi { get; init; } = [];
    public List<RataPiano> PianoAmmortamento { get; init; } = [];
}

public record ProdottoNonDisponibile(int ProdottoId, string Nome, List<string> Motivi);

public record RisultatoSimulazione
{
    public decimal? Ltv { get; init; }
    public List<Offerta> Offerte { get; init; } = [];
    public List<ProdottoNonDisponibile> NonDisponibili { get; init; } = [];
}
