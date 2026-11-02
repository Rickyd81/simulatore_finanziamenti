using Microsoft.AspNetCore.Mvc.ModelBinding;
using SimulatoreFinanziamenti.Api.Domain;

namespace SimulatoreFinanziamenti.Api.Controllers;

/// <summary>Controlli comuni alle condizioni dei prodotti e a quelle dell'anagrafica condizioni.</summary>
public static class RegoleCondizione
{
    public static void Normalizza(DatiCondizione c) => c.Codice = c.Codice.Trim().ToUpperInvariant();

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
