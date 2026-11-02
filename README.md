# Simulatore finanziamenti

Web application per il portale di una banca che permette ai clienti di simulare mutui e
prestiti e agli utenti della banca di configurare i prodotti proposti e le relative condizioni
economiche.

- **Portale clienti – Simulatore**: il cliente indica finalità, importo, valore dell'immobile,
  durata, provincia ed età; il simulatore propone i prodotti adatti ordinati per TAEG, con
  rata, TAN, TAEG, costi e piano di ammortamento scaricabile. Dal riepilogo può stampare il
  **preventivo in PDF** e inviare la
  **richiesta di finanziamento** alla banca (core banking simulato, vedi
  [docs/api-core-banking.md](docs/api-core-banking.md)).
- **Area banca – Fabbrica prodotti**: anagrafica dei prodotti (tipologia, finalità, importi,
  durate, età, LTV, province, validità) con il **condizioniere** per tassi, spese, commissioni,
  assicurazioni e imposte, anche a fasce di importo, durata e LTV. Le condizioni si inseriscono a
  mano o si importano dall'anagrafica condizioni, e restano modificabili sul singolo prodotto.
- **Area banca – Anagrafica condizioni**: condizioni pre-confezionate, raggruppabili (es. "Costi
  mutuo"), da importare nei prodotti invece di inserirle ogni volta. Il prodotto ne riceve una
  copia: modificare o eliminare una condizione in anagrafica non cambia i prodotti già configurati.
- **Area banca – Anagrafica pricing**: coppie tasso di riferimento + spread, per tipo di tasso:
  fisso (es. IRS 20 anni, tasso base prestiti) o variabile (es. Euribor 3 mesi). Il prodotto
  richiama solo i pricing del suo tipo di tasso e ne importa lo spread come copia modificabile;
  le condizioni di tasso del prodotto, se applicabili, lo sostituiscono per la loro fascia (es.
  uno spread diverso per LTV). Il pricing è obbligatorio per il variabile; un fisso senza pricing
  (prodotti delle versioni precedenti) ha il TAN direttamente nelle condizioni di tasso.

L'indagine sui simulatori esistenti da cui nascono queste scelte è in
[docs/indagine-simulatori.md](docs/indagine-simulatori.md).

## Struttura

```
backend/   API ASP.NET Core (.NET 10), EF Core su SQLite, test xUnit
frontend/  Angular 22 (componenti standalone, signal, zoneless)
docs/      documentazione funzionale
```

## Avvio in locale

Prerequisiti: .NET SDK 10 e Node.js 22.22+ o 24.15+.

```bash
# API su http://localhost:5102 (crea simulatore.db con un catalogo di esempio al primo avvio)
cd backend/SimulatoreFinanziamenti.Api
dotnet run

# Front-end su http://localhost:4200 (le chiamate /api passano dal proxy verso l'API)
cd frontend
npm install
npm start
```

Per ripartire dal catalogo di esempio basta cancellare `simulatore.db` e riavviare l'API.

## Test

```bash
cd backend && dotnet test
cd frontend && npx ng build
```

## API principali

| Metodo | Percorso | Descrizione |
| --- | --- | --- |
| POST | `/api/simulazioni` | Simula il finanziamento e restituisce offerte, prodotti esclusi e piani |
| POST | `/api/richieste-finanziamento` | Invia al core banking la richiesta per l'offerta scelta ([tracciato](docs/api-core-banking.md)) |
| GET/POST | `/api/prodotti` | Elenco e inserimento prodotti |
| GET/PUT/DELETE | `/api/prodotti/{id}` | Dettaglio, modifica, eliminazione |
| GET/POST | `/api/prodotti/{id}/condizioni` | Condizioni del prodotto |
| PUT/DELETE | `/api/prodotti/{id}/condizioni/{condizioneId}` | Modifica ed eliminazione condizione |
| GET/PUT/DELETE | `/api/pricing[/{codice}]` | Anagrafica pricing (tipo tasso, tasso di riferimento e spread) |
| GET | `/api/tabelle` | Decodifiche (finalità, categorie, province…) |

La specifica OpenAPI è pubblicata su `/openapi/v1.json`.

## Regole di calcolo

- Ammortamento alla francese con rata mensile costante; l'ultima rata assorbe gli arrotondamenti.
- TAN, uguale per fisso e variabile: tasso di riferimento del pricing + spread, dove lo spread è
  quello della condizione di categoria *Tasso* applicabile (la prima in ordine di codice), se c'è,
  altrimenti quello importato dal pricing; per il variabile si ipotizza che l'indice resti costante.
- Le condizioni percentuali si calcolano sull'importo finanziato, con eventuali minimo e massimo.
- Spese mensili addebitate con ogni rata, spese annuali con la prima rata di ogni anno.
- TAEG: tasso annuo effettivo che eguaglia l'importo erogato al netto dei costi iniziali inclusi
  nel TAEG al valore attuale di rate e spese periodiche incluse nel TAEG.
- Un prodotto è proposto se è attivo e in vigore, ammette la finalità e rispetta importo,
  durata, età, provincia e LTV; altrimenti compare tra i non disponibili con il motivo.

## Area banca e password

Fabbrica prodotti, anagrafiche condizioni e pricing richiedono una password, impostata nella configurazione
`AreaBanca:Password` (variabile d'ambiente `AreaBanca__Password`). In sviluppo, se la password
non è impostata, l'area banca è aperta; in produzione senza password resta chiusa.

## Pubblicazione

Il `Dockerfile` nella radice crea un'unica immagine: compila Angular e lo fa servire dall'API
ASP.NET Core sulla porta 8080 (o su quella indicata dalla variabile `PORT`). Il database SQLite
è in `/app/data`.

### Render

1. Accedi a [render.com](https://render.com) con il tuo account GitHub.
2. Scegli **New → Blueprint** e seleziona il repository `simulatore_finanziamenti`: Render legge
   `render.yaml` e propone il servizio `simulatore-finanziamenti` (piano gratuito, Francoforte).
3. Inserisci la password dell'area banca quando Render chiede `AreaBanca__Password`, poi conferma.
4. Al termine della build l'app è raggiungibile su `https://simulatore-finanziamenti.onrender.com`
   (o un indirizzo simile). Ogni push su `main` la ripubblica.

Sul piano gratuito il servizio si sospende dopo 15 minuti di inattività (la prima richiesta
successiva impiega circa un minuto) e il disco non è persistente: a ogni riavvio il catalogo torna
quello di esempio. Per conservare le modifiche serve un piano a pagamento con un disco montato su
`/app/data`.
