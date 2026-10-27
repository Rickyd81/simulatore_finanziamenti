# Richiesta di finanziamento verso il core banking

Dal riepilogo dell'offerta il cliente può inviare la **richiesta di finanziamento**: un pop-up
raccoglie i suoi riferimenti (nome, cognome, codice fiscale, email, telefono facoltativo e
consenso privacy) e il portale chiama l'API standard qui descritta, che compone la pratica e la
passa al core banking della banca.

## Endpoint

`POST /api/richieste-finanziamento` (pubblico, come il simulatore)

```json
{
  "prodottoId": 6,
  "simulazione": { "finalita": "Auto", "importo": 15000, "durataMesi": 48, "etaRichiedente": 35, "provincia": "RM" },
  "cliente": {
    "nome": "Mario", "cognome": "Rossi", "codiceFiscale": "RSSMRA85T10A562S",
    "email": "mario.rossi@example.com", "telefono": "+39 333 1234567", "consensoPrivacy": true
  }
}
```

- Il server **ricalcola l'offerta** con le condizioni in vigore partendo da `simulazione`: il
  portale invia solo il prodotto scelto, quindi tassi, costi e piano non possono essere alterati
  dal browser.
- `200`: pratica accettata dal core banking, con numero pratica e tracciato inviato.
- `400`: dati del cliente non validi (codice fiscale, email, consenso mancante…).
- `422`: il prodotto non è più disponibile per quei dati (es. condizioni cambiate nel frattempo).

Il contratto completo (schemi `RichiestaFinanziamento`, `EsitoRichiestaFinanziamento`,
`PraticaFinanziamento`) è pubblicato in formato OpenAPI su `/openapi/v1.json`.

## Tracciato standard: `PraticaFinanziamento` v1.0

È il messaggio che riceve il core banking. Contiene tutto ciò che serve ad aprire l'istruttoria
senza rileggere il catalogo del simulatore.

| Sezione | Contenuto |
| --- | --- |
| testata | `versione`, `idRichiesta` (GUID, utile per l'idempotenza), `dataRichiesta`, `canale` |
| `cliente` | anagrafica e contatti, età, provincia, consenso privacy |
| `prodotto` | id e codice del prodotto, tipologia, tipo di tasso; per il variabile indice, valore e spread |
| `finanziamento` | finalità, importo, durata, valore immobile e LTV, decorrenza, TAN, TAEG, rata e totali |
| `costi` | spese, commissioni e imposte con periodicità e inclusione nel TAEG |
| `pianoAmmortamento` | tutte le rate con quota capitale, interessi, spese e debito residuo |

Gli importi sono in euro con due decimali, le percentuali sono espresse in punti (5,9 = 5,9%), le
date in formato ISO 8601 e i valori codificati (`finalita`, `tipologia`, `categoria`…) usano gli
stessi codici di `/api/tabelle`.

## Esempio di risposta

(piano di ammortamento e costi accorciati)

```json
{
  "numeroPratica": "PF-20261027-432252",
  "stato": "Ricevuta",
  "dataRicezione": "2026-10-27T14:42:04.1842738+00:00",
  "messaggio": "Richiesta inviata con successo",
  "pratica": {
    "versione": "1.0",
    "idRichiesta": "2897fa47-cd98-4b75-833f-196ce3bf70a2",
    "dataRichiesta": "2026-10-27T14:42:04.184226+00:00",
    "canale": "PortaleClienti",
    "cliente": {
      "nome": "Mario",
      "cognome": "Rossi",
      "codiceFiscale": "RSSMRA85T10A562S",
      "email": "mario.rossi@example.com",
      "telefono": "+39 333 1234567",
      "eta": 35,
      "provincia": "RM",
      "consensoPrivacy": true
    },
    "prodotto": {
      "id": 6,
      "codice": "PRE-AUTO",
      "nome": "Prestito Auto",
      "tipologia": "PrestitoFinalizzato",
      "tipoTasso": "Fisso",
      "indiceRiferimento": null,
      "valoreIndice": null,
      "spread": null
    },
    "finanziamento": {
      "finalita": "Auto",
      "importo": 15000,
      "durataMesi": 48,
      "valoreImmobile": null,
      "ltv": null,
      "dataDecorrenza": "2026-11-01",
      "tan": 5.9,
      "taeg": 6.64,
      "rata": 351.59,
      "periodicitaRata": "Mensile",
      "totaleInteressi": 1876.25,
      "totaleSpese": 164,
      "importoTotaleDovuto": 17040.25
    },
    "costi": [
      {
        "codice": "BOL",
        "categoria": "ImpostaBollo",
        "descrizione": "Imposta di bollo sul contratto",
        "periodicita": "UnaTantum",
        "importo": 16,
        "importoTotale": 16,
        "inclusaNelTaeg": true
      },
      {
        "codice": "INC",
        "categoria": "CommissioneIncassoRata",
        "descrizione": "Commissione incasso rata",
        "periodicita": "Mensile",
        "importo": 1,
        "importoTotale": 48,
        "inclusaNelTaeg": true
      }
    ],
    "pianoAmmortamento": [
      {
        "numero": 1,
        "dataScadenza": "2026-12-01",
        "rata": 351.59,
        "quotaCapitale": 277.84,
        "quotaInteressi": 73.75,
        "spese": 1,
        "debitoResiduo": 14722.16
      },
      {
        "numero": 2,
        "dataScadenza": "2027-01-01",
        "rata": 351.59,
        "quotaCapitale": 279.21,
        "quotaInteressi": 72.38,
        "spese": 1,
        "debitoResiduo": 14442.95
      }
    ]
  }
}
```

## Collegamento al core banking reale

Oggi il core banking è **simulato** (`CoreBankingSimulato`): accetta ogni pratica, assegna un
numero `PF-AAAAMMGG-NNNNNN` e risponde con stato `Ricevuta`; le pratiche non vengono salvate.
Per l'integrazione reale basta scrivere un'implementazione di `ICoreBanking`
(`backend/SimulatoreFinanziamenti.Api/Services/CoreBanking.cs`) che invii la
`PraticaFinanziamento` al servizio della banca e registrarla in `Program.cs` al posto di quella
simulata: controller, tracciato e front-end restano invariati.
