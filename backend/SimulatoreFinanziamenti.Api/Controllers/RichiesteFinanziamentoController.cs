using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimulatoreFinanziamenti.Api.Data;
using SimulatoreFinanziamenti.Api.Dtos;
using SimulatoreFinanziamenti.Api.Services;

namespace SimulatoreFinanziamenti.Api.Controllers;

/// <summary>Invio al core banking della richiesta di finanziamento per l'offerta scelta dal cliente.</summary>
[ApiController]
[Route("api/richieste-finanziamento")]
public class RichiesteFinanziamentoController(SimulatoreDbContext db, MotoreSimulazione motore, ICoreBanking coreBanking, TimeProvider orologio) : ControllerBase
{
    /// <summary>
    /// Ricalcola l'offerta con le condizioni in vigore, compone la pratica nel tracciato standard
    /// e la invia al core banking. Il portale non può quindi alterare tassi o importi.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<EsitoRichiestaFinanziamento>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EsitoRichiestaFinanziamento>> Invia(RichiestaFinanziamento richiesta, CancellationToken annullamento)
    {
        var prodotti = await db.Prodotti.AsNoTracking().Include(p => p.Condizioni)
            .Where(p => p.Id == richiesta.ProdottoId && p.Attivo).ToListAsync(annullamento);
        var indici = await db.Indici.AsNoTracking().ToListAsync(annullamento);
        var simulazione = richiesta.Simulazione with
        {
            DataDecorrenza = richiesta.Simulazione.DataDecorrenza ?? DateOnly.FromDateTime(orologio.GetLocalNow().Date)
        };
        var risultato = motore.Simula(simulazione, prodotti, indici);

        if (risultato.Offerte.SingleOrDefault() is not { } offerta)
            return Problem(title: "Il prodotto scelto non è più disponibile per i dati indicati. Ripeti la simulazione.",
                statusCode: StatusCodes.Status422UnprocessableEntity);

        var pratica = ComponiPratica(richiesta.Cliente, simulazione, risultato.Ltv, offerta);
        var risposta = await coreBanking.InviaPraticaAsync(pratica, annullamento);

        return new EsitoRichiestaFinanziamento(risposta.NumeroPratica, risposta.Stato, risposta.DataRicezione,
            "Richiesta inviata con successo", pratica);
    }

    private PraticaFinanziamento ComponiPratica(DatiCliente cliente, RichiestaSimulazione s, decimal? ltv, Offerta o) => new()
    {
        IdRichiesta = Guid.NewGuid(),
        DataRichiesta = orologio.GetLocalNow(),
        Cliente = new ClientePratica
        {
            Nome = cliente.Nome.Trim(),
            Cognome = cliente.Cognome.Trim(),
            CodiceFiscale = cliente.CodiceFiscale.ToUpperInvariant(),
            Email = cliente.Email.Trim(),
            Telefono = string.IsNullOrWhiteSpace(cliente.Telefono) ? null : cliente.Telefono.Trim(),
            Eta = s.EtaRichiedente,
            Provincia = s.Provincia,
            ConsensoPrivacy = cliente.ConsensoPrivacy
        },
        Prodotto = new ProdottoPratica
        {
            Id = o.ProdottoId,
            Codice = o.Codice,
            Nome = o.Nome,
            Tipologia = o.Tipologia,
            TipoTasso = o.TipoTasso,
            IndiceRiferimento = o.IndiceRiferimento,
            ValoreIndice = o.ValoreIndice,
            Spread = o.Spread
        },
        Finanziamento = new CondizioniPratica
        {
            Finalita = s.Finalita,
            Importo = o.Importo,
            DurataMesi = o.DurataMesi,
            ValoreImmobile = s.ValoreImmobile,
            Ltv = ltv,
            DataDecorrenza = s.DataDecorrenza!.Value,
            Tan = o.Tan,
            Taeg = o.Taeg,
            Rata = o.Rata,
            TotaleInteressi = o.TotaleInteressi,
            TotaleSpese = o.TotaleSpese,
            ImportoTotaleDovuto = o.ImportoTotaleDovuto
        },
        Costi = o.Costi,
        PianoAmmortamento = o.PianoAmmortamento
    };
}
