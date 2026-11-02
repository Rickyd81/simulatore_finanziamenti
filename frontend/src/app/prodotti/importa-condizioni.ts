import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api, messaggioErrore } from '../core/api';
import { DecodificaPipe } from '../core/decodifica';
import { CondizioneAnagrafica, TipoTasso } from '../core/models';
import { descriviFasce } from '../shared/editor-condizione';

interface Gruppo {
  nome: string;
  condizioni: CondizioneAnagrafica[];
}

/** Scelta delle condizioni dell'anagrafica da importare nel condizioniere di un prodotto. */
@Component({
  selector: 'app-importa-condizioni',
  imports: [FormsModule, DecimalPipe, DecodificaPipe],
  templateUrl: './importa-condizioni.html',
})
export class ImportaCondizioni {
  private readonly api = inject(Api);

  /** Codici delle condizioni già presenti nel prodotto, che non si possono importare di nuovo. */
  readonly codiciPresenti = input.required<Set<string>>();
  readonly tipoTasso = input.required<TipoTasso>();
  readonly importa = output<number[]>();
  readonly annulla = output<void>();

  protected readonly anagrafica = signal<CondizioneAnagrafica[] | null>(null);
  protected readonly errore = signal<string | null>(null);
  protected readonly filtro = signal('');
  protected readonly selezionate = signal<Set<number>>(new Set());
  protected readonly fasce = descriviFasce;

  protected readonly gruppi = computed<Gruppo[]>(() => {
    const testo = this.filtro().trim().toLowerCase();
    const mappa = new Map<string, CondizioneAnagrafica[]>();
    for (const c of this.anagrafica() ?? []) {
      const nome = c.gruppo ?? 'Senza gruppo';
      if (testo && ![c.codice, c.descrizione, nome].some((t) => t.toLowerCase().includes(testo)))
        continue;
      mappa.set(nome, [...(mappa.get(nome) ?? []), c]);
    }
    return [...mappa].map(([nome, condizioni]) => ({ nome, condizioni }));
  });

  constructor() {
    this.api.anagraficaCondizioni().subscribe({
      next: (a) => this.anagrafica.set(a),
      error: (e) => this.errore.set(messaggioErrore(e)),
    });
  }

  protected presente(c: CondizioneAnagrafica): boolean {
    return this.codiciPresenti().has(c.codice);
  }

  protected selezionata(c: CondizioneAnagrafica): boolean {
    return this.selezionate().has(c.id);
  }

  protected alterna(c: CondizioneAnagrafica): void {
    if (this.presente(c)) return;
    this.selezionate.update((s) => {
      const nuovo = new Set(s);
      if (!nuovo.delete(c.id)) nuovo.add(c.id);
      return nuovo;
    });
  }

  private selezionabili(g: Gruppo): CondizioneAnagrafica[] {
    return g.condizioni.filter((c) => !this.presente(c));
  }

  protected gruppoSelezionato(g: Gruppo): boolean {
    const libere = this.selezionabili(g);
    return libere.length > 0 && libere.every((c) => this.selezionata(c));
  }

  protected gruppoDisponibile(g: Gruppo): boolean {
    return this.selezionabili(g).length > 0;
  }

  protected alternaGruppo(g: Gruppo): void {
    const tutte = this.gruppoSelezionato(g);
    this.selezionate.update((s) => {
      const nuovo = new Set(s);
      for (const c of this.selezionabili(g)) {
        if (tutte) nuovo.delete(c.id);
        else nuovo.add(c.id);
      }
      return nuovo;
    });
  }

  protected conferma(): void {
    if (this.selezionate().size > 0) this.importa.emit([...this.selezionate()]);
  }
}
