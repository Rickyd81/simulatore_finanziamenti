import { Component, input } from '@angular/core';

export type NomeIcona = 'modifica' | 'elimina' | 'apri' | 'scarica';

/** Icone SVG minime usate nelle liste (stessa resa su tutti i sistemi, a differenza delle emoji). */
@Component({
  selector: 'app-icona',
  template: `
    <svg width="16" height="16" viewBox="0 0 24 24" aria-hidden="true">
      @switch (nome()) {
        @case ('modifica') {
          <path
            fill="currentColor"
            d="M3 17.25V21h3.75L17.8 9.94l-3.75-3.75L3 17.25zm17.7-10.2a1 1 0 0 0 0-1.41l-2.34-2.34a1 1 0 0 0-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z"
          />
        }
        @case ('elimina') {
          <path
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
            d="M4 7h16M10 11v6M14 11v6M5 7l1 13h12l1-13M9 7V4h6v3"
          />
        }
        @case ('apri') {
          <circle cx="12" cy="12" r="10" fill="currentColor" />
          <path fill="#fff" d="M12 6.5 10.6 7.9l3.1 3.1H6.5v2h7.2l-3.1 3.1 1.4 1.4L17.5 12z" />
        }
        @case ('scarica') {
          <path
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M12 4v11m-5-5 5 5 5-5M5 20h14"
          />
        }
      }
    </svg>
  `,
  styles: `
    :host {
      display: inline-flex;
      vertical-align: middle;
    }
  `,
})
export class Icona {
  readonly nome = input.required<NomeIcona>();
}
