using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using SimulatoreFinanziamenti.Api.Data;
using SimulatoreFinanziamenti.Api.Domain;

namespace SimulatoreFinanziamenti.Api.Controllers;

/// <summary>Controlli comuni alle condizioni dei prodotti e a quelle dell'anagrafica condizioni.</summary>
public static class RegoleCondizione
{
    public static void Normalizza(DatiCondizione c) => c.Codice = c.Codice.Trim().ToUpperInvariant();

    /// <summary>
    /// Il pricing riguarda solo le condizioni di tasso, dove è obbligatorio: se il tasso di riferimento non è
    /// indicato si importa dal pricing. Con <paramref name="tipoTasso"/> (condizioni di un prodotto) il pricing
    /// deve essere dello stesso tipo di tasso del prodotto.
    /// </summary>
    public static async Task VerificaPricing(DatiCondizione c, SimulatoreDbContext db, ModelStateDictionary errori, TipoTasso? tipoTasso)
    {
        if (c.Categoria != CategoriaCondizione.Tasso)
        {
            c.CodicePricing = null;
            c.TassoRiferimento = null;
            return;
        }
        if (string.IsNullOrWhiteSpace(c.CodicePricing))
        {
            errori.AddModelError(nameof(c.CodicePricing), "Indicare il pricing della condizione di tasso");
            return;
        }
        c.CodicePricing = c.CodicePricing.Trim().ToUpperInvariant();
        var pricing = await db.Pricing.AsNoTracking().FirstOrDefaultAsync(p => p.Codice == c.CodicePricing);
        if (pricing is null)
            errori.AddModelError(nameof(c.CodicePricing), "Pricing non presente nell'anagrafica pricing");
        else if (tipoTasso is not null && pricing.TipoTasso != tipoTasso)
            errori.AddModelError(nameof(c.CodicePricing), "Il pricing scelto è di un altro tipo di tasso rispetto al prodotto");
        else c.TassoRiferimento ??= pricing.Valore;
    }

    public static void Verifica(DatiCondizione c, ModelStateDictionary errori)
    {
        if (c.Categoria == CategoriaCondizione.Tasso && c.TipoValore != TipoValore.Percentuale)
            errori.AddModelError(nameof(c.TipoValore), "Un tasso deve essere espresso in percentuale");
        if (c.FasciaImportoDa > c.FasciaImportoA)
            errori.AddModelError(nameof(c.FasciaImportoA), "Fascia di importo non valida");
        if (c.FasciaDurataDaMesi > c.FasciaDurataAMesi)
            errori.AddModelError(nameof(c.FasciaDurataAMesi), "Fascia di durata non valida");
        if (c.FasciaLtvDa > c.FasciaLtvA)
            errori.AddModelError(nameof(c.FasciaLtvA), "Fascia di LTV non valida");
        if (c.DataFineValidita < c.DataInizioValidita)
            errori.AddModelError(nameof(c.DataFineValidita), "La data di fine validità precede quella di inizio");
    }
}
