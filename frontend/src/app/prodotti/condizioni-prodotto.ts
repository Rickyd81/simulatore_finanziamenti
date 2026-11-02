import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, input, model, signal } from '@angular/core';
import { Api, messaggioErrore } from '../core/api';
import { DecodificaPipe } from '../core/decodifica';
import { Condizione } from '../core/models';
import {
  CondizioneEditabile,
  EditorCondizione,
  condizioneVuota,
  descriviFasce,
} from '../shared/editor-condizione';
import { Icona } from '../shared/icona';
import { Paginatore } from '../shared/paginatore';
import { StatoTabella } from '../shared/tabella';
import { ImportaCondizioni } from './importa-condizioni';

/**
 * Condizioniere del prodotto: elenco delle condizioni economiche e relativo dettaglio.
 * Le condizioni si inseriscono a mano o si importano dall'anagrafica condizioni.
 */
@Component({
  selector: 'app-condizioni-prodotto',
  imports: [DecimalPipe, DecodificaPipe, EditorCondizione, Icona, ImportaCondizioni, Paginatore],
  templateUrl: './condizioni-prodotto.html',
})
export class CondizioniProdotto {
  private readonly api = inject(Api);

  readonly prodottoId = input.required<number>();
  /** Il prodotto richiama un pricing: le condizioni di tasso sono spread sul tasso di riferimento. */
  readonly conPricing = input.required<boolean>();
  readonly condizioni = model.required<Condizione[]>();

  protected readonly stato = new StatoTabella<Condizione>(this.condizioni);
  /** Condizione in modifica: null = nessuna, id 0 = nuova. */
  protected readonly inModifica = signal<Condizione | null>(null);
  protected readonly importazione = signal(false);
  protected readonly errore = signal<string | null>(null);
  protected readonly avviso = signal<string | null>(null);
  protected readonly fasce = descriviFasce;
  protected readonly codici = computed(() => new Set(this.condizioni().map((c) => c.codice)));
  protected readonly importate = computed(
    () => this.condizioni().filter((c) => c.codiceAnagrafica).length,
  );

  protected nuova(): void {
    this.apri({ ...condizioneVuota(), prodottoId: this.prodottoId(), codiceAnagrafica: null });
  }

  protected apri(condizione: Condizione): void {
    this.errore.set(null);
    this.avviso.set(null);
    this.importazione.set(false);
    this.inModifica.set(condizione);
  }

  protected chiudi(): void {
    this.inModifica.set(null);
  }

  protected apriImportazione(): void {
    this.errore.set(null);
    this.avviso.set(null);
    this.inModifica.set(null);
    this.importazione.set(true);
  }

  protected nota(c: Condizione): string | null {
    return c.codiceAnagrafica
      ? `Condizione importata dall'anagrafica (${c.codiceAnagrafica}): le modifiche valgono solo per questo prodotto.`
      : null;
  }

  private aggiungi(salvate: Condizione[]): void {
    const id = new Set(salvate.map((c) => c.id));
    this.condizioni.update((elenco) =>
      [...elenco.filter((c) => !id.has(c.id)), ...salvate].sort(
        (a, b) => a.categoria.localeCompare(b.categoria) || a.codice.localeCompare(b.codice),
      ),
    );
  }

  protected salva(dati: CondizioneEditabile): void {
    const originale = this.inModifica();
    if (!originale) return;
    this.api.salvaCondizione({ ...originale, ...dati }).subscribe({
      next: (salvata) => {
        this.aggiungi([salvata]);
        this.chiudi();
      },
      error: (e) => this.errore.set(messaggioErrore(e)),
    });
  }

  protected importa(ids: number[]): void {
    this.errore.set(null);
    this.api.importaCondizioni(this.prodottoId(), ids).subscribe({
      next: (importate) => {
        this.aggiungi(importate);
        this.importazione.set(false);
        this.avviso.set(
          importate.length === 1
            ? `Importata la condizione ${importate[0].codice}.`
            : `Importate ${importate.length} condizioni: ${importate.map((c) => c.codice).join(', ')}.`,
        );
      },
      error: (e) => this.errore.set(messaggioErrore(e)),
    });
  }

  protected elimina(condizione: Condizione): void {
    if (!confirm(`Eliminare la condizione ${condizione.codice}?`)) return;
    this.api.eliminaCondizione(condizione).subscribe({
      next: () => {
        this.condizioni.update((elenco) => elenco.filter((c) => c.id !== condizione.id));
        if (this.inModifica()?.id === condizione.id) this.chiudi();
      },
      error: (e) => this.errore.set(messaggioErrore(e)),
    });
  }
}
