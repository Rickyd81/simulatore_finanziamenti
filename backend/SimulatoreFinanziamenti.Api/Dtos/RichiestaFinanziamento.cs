using System.ComponentModel.DataAnnotations;
using SimulatoreFinanziamenti.Api.Domain;

namespace SimulatoreFinanziamenti.Api.Dtos;

/// <summary>Richiesta di finanziamento inviata dal portale: l'offerta scelta e i riferimenti del cliente.</summary>
public record RichiestaFinanziamento
{
    /// <summary>Prodotto dell'offerta scelta dal cliente.</summary>
    [Range(1, int.MaxValue)]
    public int ProdottoId { get; init; }

    /// <summary>Gli stessi dati della simulazione: l'offerta viene ricalcolata dal server.</summary>
    [Required]
    public RichiestaSimulazione Simulazione { get; init; } = new();

    [Required]
    public DatiCliente Cliente { get; init; } = new();
}

public record DatiCliente
{
    [Required(ErrorMessage = "Il nome è obbligatorio"), MaxLength(60)]
    public string Nome { get; init; } = "";

    [Required(ErrorMessage = "Il cognome è obbligatorio"), MaxLength(60)]
    public string Cognome { get; init; } = "";

    [Required(ErrorMessage = "Il codice fiscale è obbligatorio")]
    [RegularExpression("^[A-Za-z]{6}[0-9LMNPQRSTUVlmnpqrstuv]{2}[A-Za-z][0-9LMNPQRSTUVlmnpqrstuv]{2}[A-Za-z][0-9LMNPQRSTUVlmnpqrstuv]{3}[A-Za-z]$",
        ErrorMessage = "Il codice fiscale non è valido")]
    public string CodiceFiscale { get; init; } = "";

    [Required(ErrorMessage = "L'email è obbligatoria"), EmailAddress(ErrorMessage = "L'email non è valida"), MaxLength(120)]
    public string Email { get; init; } = "";

    [RegularExpression(@"^\+?[0-9 ]{6,20}$", ErrorMessage = "Il numero di telefono non è valido")]
    public string? Telefono { get; init; }

    /// <summary>Consenso al trattamento dei dati personali per la gestione della richiesta.</summary>
    [Range(typeof(bool), "true", "true", ErrorMessage = "Serve il consenso al trattamento dei dati personali")]
    public bool ConsensoPrivacy { get; init; }
}

/// <summary>
/// Tracciato standard della pratica inviata al core banking (versione 1.0): contiene tutto ciò che
/// serve ad aprire l'istruttoria senza dover rileggere il catalogo prodotti del simulatore.
/// </summary>
public record PraticaFinanziamento
{
    public string Versione { get; init; } = "1.0";
    public Guid IdRichiesta { get; init; }
    public DateTimeOffset DataRichiesta { get; init; }
    public string Canale { get; init; } = "PortaleClienti";
    public ClientePratica Cliente { get; init; } = new();
    public ProdottoPratica Prodotto { get; init; } = new();
    public CondizioniPratica Finanziamento { get; init; } = new();
    public List<VoceCosto> Costi { get; init; } = [];
    public List<RataPiano> PianoAmmortamento { get; init; } = [];
}

public record ClientePratica
{
    public string Nome { get; init; } = "";
    public string Cognome { get; init; } = "";
    public string CodiceFiscale { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Telefono { get; init; }
    public int Eta { get; init; }
    public string? Provincia { get; init; }
    public bool ConsensoPrivacy { get; init; }
}

public record ProdottoPratica
{
    public int Id { get; init; }
    public string Codice { get; init; } = "";
    public string Nome { get; init; } = "";
    public TipologiaProdotto Tipologia { get; init; }
    public TipoTasso TipoTasso { get; init; }
    public string? IndiceRiferimento { get; init; }
    public decimal? ValoreIndice { get; init; }
    public decimal? Spread { get; init; }
}

public record CondizioniPratica
{
    public Finalita Finalita { get; init; }
    public decimal Importo { get; init; }
    public int DurataMesi { get; init; }
    public decimal? ValoreImmobile { get; init; }
    public decimal? Ltv { get; init; }
    public DateOnly DataDecorrenza { get; init; }
    public decimal Tan { get; init; }
    public decimal Taeg { get; init; }
    public decimal Rata { get; init; }
    public Periodicita PeriodicitaRata { get; init; } = Periodicita.Mensile;
    public decimal TotaleInteressi { get; init; }
    public decimal TotaleSpese { get; init; }
    public decimal ImportoTotaleDovuto { get; init; }
}

/// <summary>Risposta del core banking alla ricezione di una pratica.</summary>
public record RispostaCoreBanking(string NumeroPratica, string Stato, DateTimeOffset DataRicezione);

/// <summary>Esito restituito al portale, con il tracciato effettivamente inviato al core banking.</summary>
public record EsitoRichiestaFinanziamento(string NumeroPratica, string Stato, DateTimeOffset DataRicezione, string Messaggio, PraticaFinanziamento Pratica);
