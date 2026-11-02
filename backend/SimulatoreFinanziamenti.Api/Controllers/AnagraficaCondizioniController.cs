using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimulatoreFinanziamenti.Api.Data;
using SimulatoreFinanziamenti.Api.Domain;

namespace SimulatoreFinanziamenti.Api.Controllers;

/// <summary>
/// Anagrafica condizioni: condizioni economiche pre-confezionate da importare nei prodotti.
/// I prodotti ne ricevono una copia, quindi modificare o eliminare una condizione qui
/// non cambia i prodotti in cui è già stata importata.
/// </summary>
[ApiController]
[Route("api/anagrafica-condizioni")]
[Authorize(Policy = AccessoController.Politica)]
public class AnagraficaCondizioniController(SimulatoreDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<CondizioneAnagrafica>> Elenco() =>
        await db.CondizioniAnagrafica.AsNoTracking()
            .OrderBy(c => c.Gruppo).ThenBy(c => c.Categoria).ThenBy(c => c.Codice).ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CondizioneAnagrafica>> Dettaglio(int id)
    {
        var condizione = await db.CondizioniAnagrafica.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return condizione is null ? NotFound() : condizione;
    }

    [HttpPost]
    public async Task<ActionResult<CondizioneAnagrafica>> Inserisci(CondizioneAnagrafica condizione)
    {
        condizione.Id = 0;
        if (!await Valida(condizione)) return ValidationProblem(ModelState);
        db.CondizioniAnagrafica.Add(condizione);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Dettaglio), new { id = condizione.Id }, condizione);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CondizioneAnagrafica>> Aggiorna(int id, CondizioneAnagrafica dati)
    {
        var condizione = await db.CondizioniAnagrafica.FindAsync(id);
        if (condizione is null) return NotFound();
        dati.Id = id;
        if (!await Valida(dati)) return ValidationProblem(ModelState);
        db.Entry(condizione).CurrentValues.SetValues(dati);
        await db.SaveChangesAsync();
        return condizione;
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Elimina(int id)
    {
        var eliminati = await db.CondizioniAnagrafica.Where(c => c.Id == id).ExecuteDeleteAsync();
        return eliminati == 0 ? NotFound() : NoContent();
    }

    private async Task<bool> Valida(CondizioneAnagrafica c)
    {
        RegoleCondizione.Normalizza(c);
        c.Gruppo = string.IsNullOrWhiteSpace(c.Gruppo) ? null : c.Gruppo.Trim();
        if (await db.CondizioniAnagrafica.AnyAsync(x => x.Codice == c.Codice && x.Id != c.Id))
            ModelState.AddModelError(nameof(c.Codice), "Esiste già una condizione dell'anagrafica con questo codice");
        RegoleCondizione.Verifica(c, ModelState);
        return ModelState.IsValid;
    }
}
