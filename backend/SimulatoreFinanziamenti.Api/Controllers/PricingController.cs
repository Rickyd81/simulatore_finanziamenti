using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimulatoreFinanziamenti.Api.Data;
using SimulatoreFinanziamenti.Api.Domain;

namespace SimulatoreFinanziamenti.Api.Controllers;

/// <summary>
/// Anagrafica pricing: tasso di riferimento (es. IRS per il fisso, Euribor per il variabile) e spread,
/// per tipo di tasso. I prodotti ricevono una copia dello spread quando richiamano il pricing, quindi
/// modificarlo qui non cambia lo spread dei prodotti già configurati (il tasso di riferimento invece
/// vale per tutti i prodotti che usano il pricing).
/// </summary>
[ApiController]
[Route("api/pricing")]
[Authorize(Policy = AccessoController.Politica)]
public class PricingController(SimulatoreDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<Pricing>> Elenco() =>
        await db.Pricing.AsNoTracking().OrderBy(i => i.TipoTasso).ThenBy(i => i.Codice).ToListAsync();

    [HttpPut("{codice}")]
    public async Task<ActionResult<Pricing>> Salva(string codice, Pricing dati)
    {
        codice = codice.Trim().ToUpperInvariant();
        if (dati.Spread is null)
            ModelState.AddModelError(nameof(dati.Spread), "Indicare lo spread");
        if (dati.TipoTasso is null)
            ModelState.AddModelError(nameof(dati.TipoTasso), "Indicare il tipo di tasso");
        else if (await db.Prodotti.AnyAsync(p => p.CodicePricing == codice && p.TipoTasso != dati.TipoTasso))
            ModelState.AddModelError(nameof(dati.TipoTasso), "Il pricing è usato da prodotti con un altro tipo di tasso");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var pricing = await db.Pricing.FindAsync(codice);
        if (pricing is null)
        {
            pricing = new Pricing { Codice = codice };
            db.Pricing.Add(pricing);
        }
        pricing.Descrizione = dati.Descrizione;
        pricing.Valore = dati.Valore;
        pricing.DataRilevazione = dati.DataRilevazione;
        pricing.Spread = dati.Spread;
        pricing.TipoTasso = dati.TipoTasso;
        await db.SaveChangesAsync();
        return pricing;
    }

    [HttpDelete("{codice}")]
    public async Task<IActionResult> Elimina(string codice)
    {
        var eliminati = await db.Pricing.Where(i => i.Codice == codice).ExecuteDeleteAsync();
        return eliminati == 0 ? NotFound() : NoContent();
    }
}
