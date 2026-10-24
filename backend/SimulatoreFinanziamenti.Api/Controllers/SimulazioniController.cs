using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimulatoreFinanziamenti.Api.Data;
using SimulatoreFinanziamenti.Api.Dtos;
using SimulatoreFinanziamenti.Api.Services;

namespace SimulatoreFinanziamenti.Api.Controllers;

[ApiController]
[Route("api/simulazioni")]
public class SimulazioniController(SimulatoreDbContext db, MotoreSimulazione motore) : ControllerBase
{
    [HttpPost]
    public async Task<RisultatoSimulazione> Simula(RichiestaSimulazione richiesta)
    {
        var prodotti = await db.Prodotti.AsNoTracking().Include(p => p.Condizioni).Where(p => p.Attivo).ToListAsync();
        var indici = await db.Indici.AsNoTracking().ToListAsync();
        return motore.Simula(richiesta, prodotti, indici);
    }
}
