import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api, messaggioErrore } from '../core/api';
import { DecodificaPipe } from '../core/decodifica';
import { CondizioneAnagrafica } from '../core/models';
import {
  CondizioneEditabile,
  EditorCondizione,
  condizioneVuota,
  descriviFasce,
} from '../shared/editor-condizione';
import { Icona } from '../shared/icona';
import { Paginatore } from '../shared/paginatore';
import { StatoTabella } from '../shared/tabella';

/**
 * Anagrafica condizioni: condizioni economiche pre-confezionate da importare nel condizioniere
 * dei prodotti. I prodotti ne ricevono una copia, modificabile in modo indipendente.
 */
@Component({
  selector: 'app-anagrafica-condizioni',
  imports: [DecimalPipe, DecodificaPipe, EditorCondizione, FormsModule, Icona, Paginatore],
  templateUrl: './anagrafica-condizioni.html',
})
export class AnagraficaCondizioni {
  private readonly api = inject(Api);

  protected readonly condizioni = signal<CondizioneAnagrafica[]>([]);
  protected readonly filtro = signal('');
  protected readonly filtroGruppo = signal<string | null>(null);
  protected readonly inModifica = signal<CondizioneAnagrafica | null>(null);
  protected readonly errore = signal<string | null>(null);
  protected readonly fasce = descriviFasce;

  protected readonly gruppi = computed(() =>
    [
      ...new Set(
        this.condizioni()
          .map((c) => c.gruppo)
          .filter((g): g is string => !!g),
      ),
    ].sort((a, b) => a.localeCompare(b, 'it')),
  );

  protected readonly filtrate = computed(() => {
    const testo = this.filtro().trim().toLowerCase();
    const gruppo = this.filtroGruppo();
    return this.condizioni().filter(
      (c) =>
        (gruppo === null || c.gruppo === gruppo) &&
        (!testo || [c.codice, c.descrizione].some((t) => t.toLowerCase().includes(testo))),
    );
  });

  protected readonly stato = new StatoTabella<CondizioneAnagrafica>(this.filtrate);

  constructor() {
    this.api.anagraficaCondizioni().subscribe({
      next: (c) => this.condizioni.set(c),
      error: (e) => this.errore.set(messaggioErrore(e)),
    });
  }

  protected nuova(): void {
    this.apri({ ...condizioneVuota(), gruppo: this.filtroGruppo() });
  }

  protected apri(condizione: CondizioneAnagrafica): void {
    this.errore.set(null);
    this.inModifica.set(condizione);
  }

  protected salva(dati: CondizioneEditabile): void {
    const originale = this.inModifica();
    if (!originale) return;
    this.api
      .salvaCondizioneAnagrafica({ ...originale, ...dati, gruppo: dati.gruppo ?? null })
      .subscribe({
        next: (salvata) => {
          this.condizioni.update((elenco) => [
            ...elenco.filter((c) => c.id !== salvata.id),
            salvata,
          ]);
          this.inModifica.set(null);
        },
        error: (e) => this.errore.set(messaggioErrore(e)),
      });
  }

  protected elimina(condizione: CondizioneAnagrafica): void {
    if (
      !confirm(
        `Eliminare la condizione ${condizione.codice} dall'anagrafica? I prodotti in cui è già stata importata non cambiano.`,
      )
    )
      return;
    this.api.eliminaCondizioneAnagrafica(condizione.id).subscribe({
      next: () => {
        this.condizioni.update((elenco) => elenco.filter((c) => c.id !== condizione.id));
        if (this.inModifica()?.id === condizione.id) this.inModifica.set(null);
      },
      error: (e) => this.errore.set(messaggioErrore(e)),
    });
  }
}
