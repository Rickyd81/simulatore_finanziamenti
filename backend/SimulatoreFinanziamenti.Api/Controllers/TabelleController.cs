using Microsoft.AspNetCore.Mvc;
using SimulatoreFinanziamenti.Api.Domain;

namespace SimulatoreFinanziamenti.Api.Controllers;

public record Voce(string Codice, string Descrizione);

public record Tabelle(
    List<Voce> Tipologie,
    List<Voce> Finalita,
    List<Voce> TipiTasso,
    List<Voce> CategorieCondizione,
    List<Voce> TipiValore,
    List<Voce> Periodicita,
    List<Voce> Province);

/// <summary>Tabelle di decodifica con le descrizioni in italiano usate dal front-end.</summary>
[ApiController]
[Route("api/tabelle")]
public class TabelleController : ControllerBase
{
    [HttpGet]
    public Tabelle Get() => new(
        Voci<TipologiaProdotto>(new()
        {
            [TipologiaProdotto.Mutuo] = "Mutuo",
            [TipologiaProdotto.PrestitoPersonale] = "Prestito personale",
            [TipologiaProdotto.PrestitoFinalizzato] = "Prestito finalizzato"
        }),
        Voci<Finalita>(new()
        {
            [Finalita.AcquistoPrimaCasa] = "Acquisto prima casa",
            [Finalita.AcquistoSecondaCasa] = "Acquisto seconda casa",
            [Finalita.Surroga] = "Surroga (trasferimento mutuo)",
            [Finalita.Ristrutturazione] = "Ristrutturazione",
            [Finalita.Liquidita] = "Liquidità",
            [Finalita.Auto] = "Auto e moto",
            [Finalita.Arredamento] = "Arredamento",
            [Finalita.Studi] = "Studi e formazione",
            [Finalita.Altro] = "Altro"
        }),
        Voci<TipoTasso>(new() { [TipoTasso.Fisso] = "Fisso", [TipoTasso.Variabile] = "Variabile" }),
        Voci<CategoriaCondizione>(new()
        {
            [CategoriaCondizione.Tasso] = "Tasso (TAN / spread)",
            [CategoriaCondizione.SpesaIstruttoria] = "Spese di istruttoria",
            [CategoriaCondizione.SpesaPerizia] = "Spese di perizia",
            [CategoriaCondizione.CommissioneIncassoRata] = "Commissione incasso rata",
            [CategoriaCondizione.Assicurazione] = "Assicurazione",
            [CategoriaCondizione.ImpostaSostitutiva] = "Imposta sostitutiva",
            [CategoriaCondizione.ImpostaBollo] = "Imposta di bollo",
            [CategoriaCondizione.AltraSpesa] = "Altra spesa / commissione"
        }),
        Voci<TipoValore>(new() { [TipoValore.Percentuale] = "Percentuale", [TipoValore.Importo] = "Importo fisso" }),
        Voci<Periodicita>(new() { [Periodicita.UnaTantum] = "Una tantum", [Periodicita.Mensile] = "Mensile (per rata)", [Periodicita.Annuale] = "Annuale" }),
        [.. Province.Select(p => new Voce(p.Sigla, p.Nome))]);

    private static List<Voce> Voci<T>(Dictionary<T, string> descrizioni) where T : struct, Enum =>
        [.. Enum.GetValues<T>().Select(v => new Voce(v.ToString(), descrizioni.GetValueOrDefault(v, v.ToString())))];

    private static readonly (string Sigla, string Nome)[] Province =
    [
        ("AG", "Agrigento"), ("AL", "Alessandria"), ("AN", "Ancona"), ("AO", "Aosta"), ("AR", "Arezzo"), ("AP", "Ascoli Piceno"),
        ("AT", "Asti"), ("AV", "Avellino"), ("BA", "Bari"), ("BT", "Barletta-Andria-Trani"), ("BL", "Belluno"), ("BN", "Benevento"),
        ("BG", "Bergamo"), ("BI", "Biella"), ("BO", "Bologna"), ("BZ", "Bolzano"), ("BS", "Brescia"), ("BR", "Brindisi"),
        ("CA", "Cagliari"), ("CL", "Caltanissetta"), ("CB", "Campobasso"), ("CE", "Caserta"), ("CT", "Catania"), ("CZ", "Catanzaro"),
        ("CH", "Chieti"), ("CO", "Como"), ("CS", "Cosenza"), ("CR", "Cremona"), ("KR", "Crotone"), ("CN", "Cuneo"),
        ("EN", "Enna"), ("FM", "Fermo"), ("FE", "Ferrara"), ("FI", "Firenze"), ("FG", "Foggia"), ("FC", "Forlì-Cesena"),
        ("FR", "Frosinone"), ("GE", "Genova"), ("GO", "Gorizia"), ("GR", "Grosseto"), ("IM", "Imperia"), ("IS", "Isernia"),
        ("AQ", "L'Aquila"), ("SP", "La Spezia"), ("LT", "Latina"), ("LE", "Lecce"), ("LC", "Lecco"), ("LI", "Livorno"),
        ("LO", "Lodi"), ("LU", "Lucca"), ("MC", "Macerata"), ("MN", "Mantova"), ("MS", "Massa-Carrara"), ("MT", "Matera"),
        ("ME", "Messina"), ("MI", "Milano"), ("MO", "Modena"), ("MB", "Monza e Brianza"), ("NA", "Napoli"), ("NO", "Novara"),
        ("NU", "Nuoro"), ("OR", "Oristano"), ("PD", "Padova"), ("PA", "Palermo"), ("PR", "Parma"), ("PV", "Pavia"),
        ("PG", "Perugia"), ("PU", "Pesaro e Urbino"), ("PE", "Pescara"), ("PC", "Piacenza"), ("PI", "Pisa"), ("PT", "Pistoia"),
        ("PN", "Pordenone"), ("PZ", "Potenza"), ("PO", "Prato"), ("RG", "Ragusa"), ("RA", "Ravenna"), ("RC", "Reggio Calabria"),
        ("RE", "Reggio Emilia"), ("RI", "Rieti"), ("RN", "Rimini"), ("RM", "Roma"), ("RO", "Rovigo"), ("SA", "Salerno"),
        ("SS", "Sassari"), ("SV", "Savona"), ("SI", "Siena"), ("SR", "Siracusa"), ("SO", "Sondrio"), ("SU", "Sud Sardegna"),
        ("TA", "Taranto"), ("TE", "Teramo"), ("TR", "Terni"), ("TO", "Torino"), ("TP", "Trapani"), ("TN", "Trento"),
        ("TV", "Treviso"), ("TS", "Trieste"), ("UD", "Udine"), ("VA", "Varese"), ("VE", "Venezia"), ("VB", "Verbano-Cusio-Ossola"),
        ("VC", "Vercelli"), ("VR", "Verona"), ("VV", "Vibo Valentia"), ("VI", "Vicenza"), ("VT", "Viterbo")
    ];
}
