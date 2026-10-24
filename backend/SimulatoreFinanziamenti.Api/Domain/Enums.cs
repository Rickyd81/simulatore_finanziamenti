namespace SimulatoreFinanziamenti.Api.Domain;

public enum TipologiaProdotto
{
    Mutuo,
    PrestitoPersonale,
    PrestitoFinalizzato
}

public enum Finalita
{
    AcquistoPrimaCasa,
    AcquistoSecondaCasa,
    Surroga,
    Ristrutturazione,
    Liquidita,
    Auto,
    Arredamento,
    Studi,
    Altro
}

public static class FinalitaExtensions
{
    /// <summary>
    /// Finalità legate a un immobile da acquistare o già ipotecato, per le quali il cliente
    /// indica il valore dell'immobile (stesso elenco di FINALITA_MUTUO nel front-end).
    /// </summary>
    public static bool RichiedeImmobile(this Finalita finalita) =>
        finalita is Finalita.AcquistoPrimaCasa or Finalita.AcquistoSecondaCasa or Finalita.Surroga;
}

public enum TipoTasso
{
    Fisso,
    Variabile
}

public enum CategoriaCondizione
{
    Tasso,
    SpesaIstruttoria,
    SpesaPerizia,
    CommissioneIncassoRata,
    Assicurazione,
    ImpostaSostitutiva,
    ImpostaBollo,
    AltraSpesa
}

/// <summary>Una condizione percentuale si calcola sempre sull'importo finanziato.</summary>
public enum TipoValore
{
    Percentuale,
    Importo
}

public enum Periodicita
{
    UnaTantum,
    Mensile,
    Annuale
}
