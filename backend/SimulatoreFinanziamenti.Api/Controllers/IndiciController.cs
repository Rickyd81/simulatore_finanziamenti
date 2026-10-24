using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimulatoreFinanziamenti.Api.Data;
using SimulatoreFinanziamenti.Api.Domain;

namespace SimulatoreFinanziamenti.Api.Controllers;

/// <summary>Indici di riferimento per i tassi variabili (es. Euribor).</summary>
[ApiController]
[Route("api/indici")]
[Authorize(Policy = AccessoController.Politica)]
public class IndiciController(SimulatoreDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<IndiceRiferimento>> Elenco() =>
        await db.Indici.AsNoTracking().OrderBy(i => i.Codice).ToListAsync();

    [HttpPut("{codice}")]
    public async Task<IndiceRiferimento> Salva(string codice, IndiceRiferimento dati)
    {
        codice = codice.Trim().ToUpperInvariant();
        var indice = await db.Indici.FindAsync(codice);
        if (indice is null)
        {
            indice = new IndiceRiferimento { Codice = codice };
            db.Indici.Add(indice);
        }
        indice.Descrizione = dati.Descrizione;
        indice.Valore = dati.Valore;
        indice.DataRilevazione = dati.DataRilevazione;
        await db.SaveChangesAsync();
        return indice;
    }

    [HttpDelete("{codice}")]
    public async Task<IActionResult> Elimina(string codice)
    {
        var eliminati = await db.Indici.Where(i => i.Codice == codice).ExecuteDeleteAsync();
        return eliminati == 0 ? NotFound() : NoContent();
    }
}
