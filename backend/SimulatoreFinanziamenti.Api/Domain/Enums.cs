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
