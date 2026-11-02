using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimulatoreFinanziamenti.Api.Data;
using SimulatoreFinanziamenti.Api.Domain;

namespace SimulatoreFinanziamenti.Api.Controllers;

/// <summary>Fabbrica prodotti: anagrafica dei prodotti e relativo condizioniere.</summary>
[ApiController]
[Route("api/prodotti")]
[Authorize(Policy = AccessoController.Politica)]
public class ProdottiController(SimulatoreDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<Prodotto>> Elenco() =>
        await db.Prodotti.AsNoTracking().OrderBy(p => p.Codice).ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Prodotto>> Dettaglio(int id)
    {
        var prodotto = await db.Prodotti.AsNoTracking().Include(p => p.Condizioni).FirstOrDefaultAsync(p => p.Id == id);
        if (prodotto is null) return NotFound();
        prodotto.Condizioni = [.. prodotto.Condizioni.OrderBy(c => c.Categoria).ThenBy(c => c.Codice)];
        return prodotto;
    }

    [HttpPost]
    public async Task<ActionResult<Prodotto>> Inserisci(Prodotto prodotto)
    {
        prodotto.Id = 0;
        prodotto.Condizioni = [];
        if (!await Valida(prodotto)) return ValidationProblem(ModelState);
        db.Prodotti.Add(prodotto);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Dettaglio), new { id = prodotto.Id }, prodotto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Prodotto>> Aggiorna(int id, Prodotto dati)
    {
        var prodotto = await db.Prodotti.FindAsync(id);
        if (prodotto is null) return NotFound();
        dati.Id = id;
        if (!await Valida(dati)) return ValidationProblem(ModelState);

        db.Entry(prodotto).CurrentValues.SetValues(dati);
        prodotto.FinalitaAmmesse = dati.FinalitaAmmesse;
        prodotto.ProvinceAmmesse = dati.ProvinceAmmesse;
        await db.SaveChangesAsync();
        return prodotto;
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Elimina(int id)
    {
        var eliminati = await db.Prodotti.Where(p => p.Id == id).ExecuteDeleteAsync();
        return eliminati == 0 ? NotFound() : NoContent();
    }

    [HttpGet("{id:int}/condizioni")]
    public async Task<ActionResult<List<Condizione>>> Condizioni(int id)
    {
        if (!await db.Prodotti.AnyAsync(p => p.Id == id)) return NotFound();
        return await db.Condizioni.AsNoTracking().Where(c => c.ProdottoId == id)
            .OrderBy(c => c.Categoria).ThenBy(c => c.Codice).ToListAsync();
    }

    [HttpGet("{id:int}/condizioni/{condizioneId:int}")]
    public async Task<ActionResult<Condizione>> Condizione(int id, int condizioneId)
    {
        var condizione = await db.Condizioni.AsNoTracking().FirstOrDefaultAsync(c => c.Id == condizioneId && c.ProdottoId == id);
        return condizione is null ? NotFound() : condizione;
    }

    [HttpPost("{id:int}/condizioni")]
    public async Task<ActionResult<Condizione>> InserisciCondizione(int id, Condizione condizione)
    {
        if (!await db.Prodotti.AnyAsync(p => p.Id == id)) return NotFound();
        condizione.Id = 0;
        condizione.ProdottoId = id;
        condizione.CodiceAnagrafica = null;
        if (!await Valida(condizione)) return ValidationProblem(ModelState);
        db.Condizioni.Add(condizione);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Condizione), new { id, condizioneId = condizione.Id }, condizione);
    }

    /// <summary>
    /// Importa nel prodotto una copia delle condizioni indicate dell'anagrafica condizioni.
    /// Le condizioni importate si possono poi modificare come quelle inserite a mano.
    /// </summary>
    [HttpPost("{id:int}/condizioni/importa")]
    public async Task<ActionResult<List<Condizione>>> ImportaCondizioni(int id, ImportazioneCondizioni richiesta)
    {
        if (!await db.Prodotti.AnyAsync(p => p.Id == id)) return NotFound();
        var ids = richiesta.CondizioniAnagrafica.Distinct().ToList();
        if (ids.Count == 0)
        {
            ModelState.AddModelError(nameof(richiesta.CondizioniAnagrafica), "Selezionare almeno una condizione dell'anagrafica");
            return ValidationProblem(ModelState);
        }

        var modelli = await db.CondizioniAnagrafica.AsNoTracking().Where(a => ids.Contains(a.Id)).ToListAsync();
        if (modelli.Count != ids.Count)
        {
            ModelState.AddModelError(nameof(richiesta.CondizioniAnagrafica), "Alcune condizioni selezionate non sono più presenti nell'anagrafica");
            return ValidationProblem(ModelState);
        }

        var codici = modelli.Select(a => a.Codice).ToList();
        var giaPresenti = await db.Condizioni.Where(c => c.ProdottoId == id && codici.Contains(c.Codice))
            .OrderBy(c => c.Codice).Select(c => c.Codice).ToListAsync();
        if (giaPresenti.Count > 0)
        {
            ModelState.AddModelError(nameof(richiesta.CondizioniAnagrafica),
                $"Il prodotto ha già condizioni con codice {string.Join(", ", giaPresenti)}: modificale o eliminale prima di importarle");
            return ValidationProblem(ModelState);
        }

        var importate = modelli.Select(a => Domain.Condizione.DaAnagrafica(a, id)).OrderBy(c => c.Categoria).ThenBy(c => c.Codice).ToList();
        db.Condizioni.AddRange(importate);
        await db.SaveChangesAsync();
        return importate;
    }

    [HttpPut("{id:int}/condizioni/{condizioneId:int}")]
    public async Task<ActionResult<Condizione>> AggiornaCondizione(int id, int condizioneId, Condizione dati)
    {
        var condizione = await db.Condizioni.FirstOrDefaultAsync(c => c.Id == condizioneId && c.ProdottoId == id);
        if (condizione is null) return NotFound();
        dati.Id = condizioneId;
        dati.ProdottoId = id;
        // L'origine dall'anagrafica resta quella dell'importazione anche se la condizione viene modificata.
        dati.CodiceAnagrafica = condizione.CodiceAnagrafica;
        if (!await Valida(dati)) return ValidationProblem(ModelState);
        db.Entry(condizione).CurrentValues.SetValues(dati);
        await db.SaveChangesAsync();
        return condizione;
    }

    [HttpDelete("{id:int}/condizioni/{condizioneId:int}")]
    public async Task<IActionResult> EliminaCondizione(int id, int condizioneId)
    {
        var eliminati = await db.Condizioni.Where(c => c.Id == condizioneId && c.ProdottoId == id).ExecuteDeleteAsync();
        return eliminati == 0 ? NotFound() : NoContent();
    }

    private async Task<bool> Valida(Prodotto p)
    {
        p.Codice = p.Codice.Trim().ToUpperInvariant();
        p.ProvinceAmmesse = [.. p.ProvinceAmmesse.Select(s => s.Trim().ToUpperInvariant()).Where(s => s.Length > 0).Distinct()];
        p.FinalitaAmmesse = [.. p.FinalitaAmmesse.Distinct()];

        if (await db.Prodotti.AnyAsync(x => x.Codice == p.Codice && x.Id != p.Id))
            ModelState.AddModelError(nameof(p.Codice), "Esiste già un prodotto con questo codice");
        if (p.FinalitaAmmesse.Count == 0)
            ModelState.AddModelError(nameof(p.FinalitaAmmesse), "Indicare almeno una finalità");
        if (p.ImportoMinimo > p.ImportoMassimo)
            ModelState.AddModelError(nameof(p.ImportoMassimo), "L'importo massimo deve essere maggiore del minimo");
        if (p.DurataMinimaMesi > p.DurataMassimaMesi)
            ModelState.AddModelError(nameof(p.DurataMassimaMesi), "La durata massima deve essere maggiore della minima");
        if (p.DataFineValidita < p.DataInizioValidita)
            ModelState.AddModelError(nameof(p.DataFineValidita), "La data di fine validità precede quella di inizio");
        if (p.TipoTasso == TipoTasso.Fisso)
        {
            p.CodicePricing = null;
            p.Spread = null;
        }
        else if (string.IsNullOrWhiteSpace(p.CodicePricing))
            ModelState.AddModelError(nameof(p.CodicePricing), "Un prodotto a tasso variabile richiede un pricing");
        else
        {
            p.CodicePricing = p.CodicePricing.Trim().ToUpperInvariant();
            var pricing = await db.Pricing.AsNoTracking().FirstOrDefaultAsync(x => x.Codice == p.CodicePricing);
            if (pricing is null)
                ModelState.AddModelError(nameof(p.CodicePricing), "Pricing non presente nell'anagrafica pricing");
            // Richiamando il pricing se ne importa lo spread, se non è già stato indicato sul prodotto.
            else p.Spread ??= pricing.Spread;
        }
        return ModelState.IsValid;
    }

    private async Task<bool> Valida(Condizione c)
    {
        RegoleCondizione.Normalizza(c);
        if (await db.Condizioni.AnyAsync(x => x.ProdottoId == c.ProdottoId && x.Codice == c.Codice && x.Id != c.Id))
            ModelState.AddModelError(nameof(c.Codice), "Esiste già una condizione con questo codice per il prodotto");
        RegoleCondizione.Verifica(c, ModelState);
        return ModelState.IsValid;
    }
}

/// <summary>Condizioni dell'anagrafica (per id) da importare in un prodotto.</summary>
public record ImportazioneCondizioni(List<int> CondizioniAnagrafica);
