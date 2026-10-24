# Simulatore finanziamenti

Web application per il portale di una banca che permette ai clienti di simulare mutui e
prestiti e agli utenti della banca di configurare i prodotti proposti e le relative condizioni
economiche.

- **Portale clienti – Simulatore**: il cliente indica finalità, importo, valore dell'immobile,
  durata, provincia ed età; il simulatore propone i prodotti adatti ordinati per TAEG, con
  rata, TAN, TAEG, costi e piano di ammortamento scaricabile.
- **Area banca – Fabbrica prodotti**: anagrafica dei prodotti (tipologia, finalità, importi,
  durate, età, LTV, province, validità) con il **condizioniere** per tassi, spese, commissioni,
  assicurazioni e imposte, anche a fasce di importo, durata e LTV.
- **Area banca – Indici di riferimento**: valore dell'Euribor e degli altri indici usati dai
  prodotti a tasso variabile.

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
| GET/POST | `/api/prodotti` | Elenco e inserimento prodotti |
| GET/PUT/DELETE | `/api/prodotti/{id}` | Dettaglio, modifica, eliminazione |
| GET/POST | `/api/prodotti/{id}/condizioni` | Condizioni del prodotto |
| PUT/DELETE | `/api/prodotti/{id}/condizioni/{condizioneId}` | Modifica ed eliminazione condizione |
| GET/PUT/DELETE | `/api/indici[/{codice}]` | Indici di riferimento |
| GET | `/api/tabelle` | Decodifiche (finalità, categorie, province…) |

In sviluppo la specifica OpenAPI è su `/openapi/v1.json`.

## Regole di calcolo

- Ammortamento alla francese con rata mensile costante; l'ultima rata assorbe gli arrotondamenti.
- TAN: condizione di categoria *Tasso* applicabile (la prima in ordine di codice); per i
  prodotti variabili è indice + spread, con l'ipotesi che l'indice resti costante.
- Le condizioni percentuali si calcolano sull'importo finanziato, con eventuali minimo e massimo.
- Spese mensili addebitate con ogni rata, spese annuali con la prima rata di ogni anno.
- TAEG: tasso annuo effettivo che eguaglia l'importo erogato al netto dei costi iniziali inclusi
  nel TAEG al valore attuale di rate e spese periodiche incluse nel TAEG.
- Un prodotto è proposto se è attivo e in vigore, ammette la finalità e rispetta importo,
  durata, età, provincia e LTV; altrimenti compare tra i non disponibili con il motivo.

## Limiti attuali

L'area banca non ha ancora autenticazione: va protetta prima della pubblicazione sul portale.
