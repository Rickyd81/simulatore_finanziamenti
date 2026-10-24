import { HttpClient, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Observable, catchError, map, of, tap, throwError } from 'rxjs';

interface StatoAccesso {
  autenticato: boolean;
  passwordRichiesta: boolean;
}

/** Stato dell'accesso all'area banca (cookie di sessione gestito dal back-end). */
@Injectable({ providedIn: 'root' })
export class Accesso {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/accesso';

  readonly autenticato = signal(false);
  readonly passwordRichiesta = signal(true);

  verifica(): Observable<boolean> {
    return this.http.get<StatoAccesso>(this.url).pipe(
      tap((s) => {
        this.autenticato.set(s.autenticato);
        this.passwordRichiesta.set(s.passwordRichiesta);
      }),
      map((s) => s.autenticato),
      catchError(() => of(false)),
    );
  }

  entra(password: string): Observable<void> {
    return this.http.post<void>(this.url, { password }).pipe(tap(() => this.autenticato.set(true)));
  }

  esci(): Observable<void> {
    return this.http.delete<void>(this.url).pipe(tap(() => this.autenticato.set(false)));
  }
}

/** Le pagine dell'area banca richiedono l'accesso. */
export const areaBancaGuard: CanActivateFn = (_, stato) => {
  const router = inject(Router);
  return inject(Accesso)
    .verifica()
    .pipe(
      map(
        (ok) => ok || router.createUrlTree(['/accesso'], { queryParams: { ritorno: stato.url } }),
      ),
    );
};

/** Se la sessione scade, una chiamata dell'area banca riceve 401: si torna alla pagina di accesso. */
export const sessioneScadutaInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const accesso = inject(Accesso);
  return next(req).pipe(
    catchError((errore: unknown) => {
      if (
        errore instanceof HttpErrorResponse &&
        errore.status === 401 &&
        !req.url.startsWith('/api/accesso')
      ) {
        accesso.autenticato.set(false);
        router.navigate(['/accesso'], { queryParams: { ritorno: router.url } });
      }
      return throwError(() => errore);
    }),
  );
};
