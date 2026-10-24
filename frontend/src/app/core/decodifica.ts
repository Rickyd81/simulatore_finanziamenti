import { Pipe, PipeTransform, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Api } from './api';
import { Tabelle } from './models';

/**
 * Restituisce la descrizione italiana di un codice, es. `'AcquistoPrimaCasa' | decodifica: 'finalita'`.
 * È impura perché le tabelle arrivano in modo asincrono dal back-end.
 */
@Pipe({ name: 'decodifica', pure: false })
export class DecodificaPipe implements PipeTransform {
  private readonly tabelle = toSignal(inject(Api).tabelle$);

  transform(codice: string | null | undefined, tabella: keyof Tabelle): string {
    if (!codice) return '';
    return this.tabelle()?.[tabella].find((v) => v.codice === codice)?.descrizione ?? codice;
  }
}
