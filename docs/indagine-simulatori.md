# Indagine sui simulatori di finanziamento delle banche italiane

Breve ricognizione dei simulatori pubblici di mutui e prestiti (siti di banche e comparatori)
fatta per definire i dati da chiedere al cliente, i risultati da mostrare e le regole da
rendere configurabili nella fabbrica prodotti.

## Cosa chiedono i simulatori di mutuo

I simulatori delle grandi banche (es. Intesa Sanpaolo, UniCredit, BPER, BNL) e dei comparatori
(Facile.it, MutuiOnline, SOStariffe) chiedono in sostanza gli stessi dati:

| Dato | Uso nel calcolo |
| --- | --- |
| Finalità (acquisto prima casa, seconda casa, surroga, ristrutturazione, liquidità) | Seleziona i prodotti e le imposte (0,25% prima casa, 2% seconda casa, surroga esente) |
| Valore dell'immobile e importo richiesto | Calcolo del Loan To Value (LTV), di solito max 80%; fino al 100% per gli under 36 con garanzia Consap |
| Durata (di solito da 5 a 30 anni, a passi di 5) | Rata e fascia di tasso (i tassi crescono con la durata) |
| Tipo di tasso (fisso, variabile, misto) | Il variabile è indice Euribor + spread |
| Età del richiedente | Età minima/massima e limite di età a fine mutuo (tipicamente 75-80 anni) |
| Provincia/comune dell'immobile | Offerte territoriali e calcolo perizia |
| Reddito e tipo di contratto (solo alcuni) | Sostenibilità della rata (rapporto rata/reddito) |

## Cosa mostrano come risultato

- Elenco delle offerte ordinabile per rata o per TAEG, con evidenza della più conveniente.
- Per ogni offerta: rata mensile, TAN, TAEG, importo totale dovuto, spese di istruttoria,
  perizia, imposta sostitutiva, assicurazione incendio e scoppio, commissione incasso rata.
- Per il variabile: indice di riferimento e spread, con l'avvertenza che la rata può cambiare.
- Piano di ammortamento alla francese (rata costante), spesso scaricabile.
- Avvertenze di trasparenza: la simulazione non è un'offerta, rinvio ai fogli informativi.

## Prestiti personali e finalizzati

I simulatori di prestito (banche e società di credito al consumo) chiedono finalità
(liquidità, auto, arredamento, ristrutturazione, studi), importo e durata in mesi (di solito
12-120). Mostrano rata, TAN, TAEG, spese di istruttoria, imposta di bollo, commissione incasso
rata e importo totale dovuto, come richiesto dalla normativa sul credito ai consumatori.
I tassi sono spesso a fasce di importo (più basso sopra una certa soglia).

## Scelte fatte nel prodotto

- **Fabbrica prodotti**: per ogni prodotto si configurano tipologia, finalità ammesse, importi,
  durate, età (minima, massima alla richiesta, massima a fine finanziamento), LTV massimo,
  province ammesse e periodo di validità.
- **Condizioniere**: ogni condizione ha una categoria (tasso, istruttoria, perizia, incasso
  rata, assicurazione, imposta sostitutiva, bollo, altre spese), un valore in percentuale o in
  euro (con minimo e massimo), una periodicità (una tantum, mensile, annuale), l'inclusione nel
  TAEG e fasce facoltative di importo, durata, LTV e finalità. Così si esprimono sia i tassi a
  griglia (durata × LTV) sia le imposte diverse per prima e seconda casa.
- **Indici di riferimento**: il valore dell'Euribor si aggiorna in un solo punto e vale per
  tutti i prodotti variabili.
- **Simulatore**: chiede finalità, importo, valore immobile (solo mutui), durata, provincia ed
  età; mostra le offerte ordinate per TAEG, i prodotti esclusi con il motivo, il riepilogo
  dei costi e il piano di ammortamento scaricabile in CSV.

Rimandati a una fase successiva: tasso misto, reddito e sostenibilità della rata,
preammortamento, rata non mensile, offerte personalizzate per segmento di cliente.

## Fonti consultate

- [Intesa Sanpaolo, come funziona il simulatore mutuo](https://www.intesasanpaolo.com/it/persone-e-famiglie/prodotti/mutui/come-funziona-simulatore-mutuo.html)
- [Facile.it, preventivo mutuo](https://www.facile.it/mutui/preventivo.html?idFinalita=2)
- [Facile.it, simulazione prestito](https://www.facile.it/prestiti/preventivo-simulazione-prestito.html)
- [Facile.it, come calcolare la rata di un prestito](https://www.facile.it/prestiti/guida/come-calcolare-la-rata-di-un-prestito.html)
- [SOStariffe, calcolo rata mutuo](https://www.sostariffe.it/mutui/guide/calcolo-rata-mutuo-come-fare-simulazione-ed-esempi)
- [ComparaSemplice, formula della rata del mutuo](https://www.comparasemplice.it/mutui-online/calcolo-rata-mutuo)
- [Studio Cataldi, prestito personale o finalizzato](https://www.studiocataldi.it/articoli/29230-prestito-personale-o-finalizzato-qual-e-il-piu-conveniente.asp)
