import { Component, input } from '@angular/core';
import { StatoTabella } from './tabella';

@Component({
  selector: 'app-paginatore',
  template: `
    <div class="paginatore">
      <span
        >Pagina {{ stato().totale() ? stato().pagina() : 0 }} di
        {{ stato().totale() ? stato().pagine() : 0 }} (righe totali {{ stato().totale() }})</span
      >
      <button
        type="button"
        class="btn-icona"
        title="Prima pagina"
        [disabled]="stato().pagina() <= 1"
        (click)="stato().pagina.set(1)"
      >
        «
      </button>
      <button
        type="button"
        class="btn-icona"
        title="Pagina precedente"
        [disabled]="stato().pagina() <= 1"
        (click)="stato().pagina.set(stato().pagina() - 1)"
      >
        ‹
      </button>
      <button
        type="button"
        class="btn-icona"
        title="Pagina successiva"
        [disabled]="stato().pagina() >= stato().pagine()"
        (click)="stato().pagina.set(stato().pagina() + 1)"
      >
        ›
      </button>
      <button
        type="button"
        class="btn-icona"
        title="Ultima pagina"
        [disabled]="stato().pagina() >= stato().pagine()"
        (click)="stato().pagina.set(stato().pagine())"
      >
        »
      </button>
      <select
        aria-label="Righe per pagina"
        [value]="stato().dimensione()"
        (change)="cambiaDimensione($event)"
      >
        @for (n of [10, 25, 50, 100]; track n) {
          <option [value]="n">{{ n }}</option>
        }
      </select>
    </div>
  `,
})
export class Paginatore {
  readonly stato = input.required<StatoTabella<any>>();

  cambiaDimensione(evento: Event): void {
    this.stato().dimensione.set(Number((evento.target as HTMLSelectElement).value));
    this.stato().pagina.set(1);
  }
}
