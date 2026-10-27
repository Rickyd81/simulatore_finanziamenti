import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';
import {
  Condizione,
  EsitoRichiestaFinanziamento,
  IndiceRiferimento,
  Prodotto,
  RichiestaFinanziamento,
  RichiestaSimulazione,
  RisultatoSimulazione,
  Tabelle,
} from './models';

@Injectable({ providedIn: 'root' })
export class Api {
  private readonly http = inject(HttpClient);
  private readonly base = '/api';

  /** Le tabelle di decodifica non cambiano: si scaricano una volta sola. */
  readonly tabelle$ = this.http.get<Tabelle>(`${this.base}/tabelle`).pipe(shareReplay(1));

  prodotti(): Observable<Prodotto[]> {
    return this.http.get<Prodotto[]>(`${this.base}/prodotti`);
  }

  prodotto(id: number): Observable<Prodotto> {
    return this.http.get<Prodotto>(`${this.base}/prodotti/${id}`);
  }

  salvaProdotto(prodotto: Prodotto): Observable<Prodotto> {
    return prodotto.id
      ? this.http.put<Prodotto>(`${this.base}/prodotti/${prodotto.id}`, prodotto)
      : this.http.post<Prodotto>(`${this.base}/prodotti`, prodotto);
  }

  eliminaProdotto(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/prodotti/${id}`);
  }

  salvaCondizione(condizione: Condizione): Observable<Condizione> {
    const url = `${this.base}/prodotti/${condizione.prodottoId}/condizioni`;
    return condizione.id
      ? this.http.put<Condizione>(`${url}/${condizione.id}`, condizione)
      : this.http.post<Condizione>(url, condizione);
  }

  eliminaCondizione(condizione: Condizione): Observable<void> {
    return this.http.delete<void>(
      `${this.base}/prodotti/${condizione.prodottoId}/condizioni/${condizione.id}`,
    );
  }

  indici(): Observable<IndiceRiferimento[]> {
    return this.http.get<IndiceRiferimento[]>(`${this.base}/indici`);
  }

  salvaIndice(indice: IndiceRiferimento): Observable<IndiceRiferimento> {
    return this.http.put<IndiceRiferimento>(
      `${this.base}/indici/${encodeURIComponent(indice.codice)}`,
      indice,
    );
  }

  eliminaIndice(codice: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/indici/${encodeURIComponent(codice)}`);
  }

  simula(richiesta: RichiestaSimulazione): Observable<RisultatoSimulazione> {
    return this.http.post<RisultatoSimulazione>(`${this.base}/simulazioni`, richiesta);
  }

  inviaRichiestaFinanziamento(
    richiesta: RichiestaFinanziamento,
  ): Observable<EsitoRichiestaFinanziamento> {
    return this.http.post<EsitoRichiestaFinanziamento>(
      `${this.base}/richieste-finanziamento`,
      richiesta,
    );
  }
}

/** Trasforma un errore HTTP (anche i ValidationProblem di ASP.NET) in un messaggio leggibile. */
export function messaggioErrore(errore: unknown): string {
  if (errore instanceof HttpErrorResponse) {
    const errori = errore.error?.errors as Record<string, string[]> | undefined;
    if (errori) return Object.values(errori).flat().join(' ');
    if (errore.status === 0) return 'Il servizio non è raggiungibile. Riprova più tardi.';
    return errore.error?.title ?? `Errore ${errore.status}`;
  }
  return 'Si è verificato un errore imprevisto.';
}
