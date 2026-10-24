import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, input, model, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Api, messaggioErrore } from '../core/api';
import { DecodificaPipe } from '../core/decodifica';
import {
  CategoriaCondizione,
  Condizione,
  Finalita,
  Periodicita,
  TipoTasso,
  TipoValore,
} from '../core/models';
import { Icona } from '../shared/icona';
import { Paginatore } from '../shared/paginatore';
import { StatoTabella } from '../shared/tabella';

/** Condizioniere del prodotto: elenco delle condizioni economiche e relativo dettaglio. */
@Component({
  selector: 'app-condizioni-prodotto',
  imports: [ReactiveFormsModule, DecimalPipe, DecodificaPipe, Icona, Paginatore],
  templateUrl: './condizioni-prodotto.html',
})
export class CondizioniProdotto {
  private readonly api = inject(Api);
  private readonly fb = inject(FormBuilder);

  readonly prodottoId = input.required<number>();
  readonly tipoTasso = input.required<TipoTasso>();
  readonly condizioni = model.required<Condizione[]>();

  protected readonly tabelle = toSignal(this.api.tabelle$);
  protected readonly stato = new StatoTabella<Condizione>(this.condizioni);
  /** Condizione in modifica: null = nessuna, id 0 = nuova. */
  protected readonly inModifica = signal<Condizione | null>(null);
  protected readonly errore = signal<string | null>(null);
  protected readonly chiuse = signal<Set<string>>(new Set());

  protected readonly form = this.fb.group({
    codice: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(20)]),
    categoria: this.fb.control<CategoriaCondizione | null>(null, Validators.required),
    descrizione: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(200)]),
    tipoValore: this.fb.nonNullable.control<TipoValore>('Percentuale'),
    valore: this.fb.control<number | null>(null, Validators.required),
    importoMinimo: this.fb.control<number | null>(null),
    importoMassimo: this.fb.control<number | null>(null),
    periodicita: this.fb.nonNullable.control<Periodicita>('UnaTantum'),
    inclusaNelTaeg: this.fb.nonNullable.control(true),
    fasciaImportoDa: this.fb.control<number | null>(null),
    fasciaImportoA: this.fb.control<number | null>(null),
    fasciaDurataDaMesi: this.fb.control<number | null>(null),
    fasciaDurataAMesi: this.fb.control<number | null>(null),
    fasciaLtvDa: this.fb.control<number | null>(null),
    fasciaLtvA: this.fb.control<number | null>(null),
    finalita: this.fb.control<Finalita | null>(null),
    dataInizioValidita: this.fb.nonNullable.control('', Validators.required),
    dataFineValidita: this.fb.control<string | null>(null),
  });

  private readonly categoria = toSignal(this.form.controls.categoria.valueChanges);
  private readonly tipoValore = toSignal(this.form.controls.tipoValore.valueChanges);
  protected readonly tasso = computed(() => this.categoria() === 'Tasso');
  protected readonly percentuale = computed(() => this.tipoValore() === 'Percentuale');

  protected nuova(): void {
    this.apri({
      id: 0,
      prodottoId: this.prodottoId(),
      codice: '',
      categoria: null as unknown as CategoriaCondizione,
      descrizione: '',
      tipoValore: 'Percentuale',
      valore: 0,
      importoMinimo: null,
      importoMassimo: null,
      periodicita: 'UnaTantum',
      inclusaNelTaeg: true,
      fasciaImportoDa: null,
      fasciaImportoA: null,
      fasciaDurataDaMesi: null,
      fasciaDurataAMesi: null,
      fasciaLtvDa: null,
      fasciaLtvA: null,
      finalita: null,
      dataInizioValidita: new Date().toISOString().slice(0, 10),
      dataFineValidita: null,
    });
  }

  protected apri(condizione: Condizione): void {
    this.errore.set(null);
    this.inModifica.set(condizione);
    this.form.reset(condizione);
    // Il codice di una condizione esistente non si modifica, come nella schermata di riferimento.
    if (condizione.id) this.form.controls.codice.disable();
    else this.form.controls.codice.enable();
  }

  protected chiudi(): void {
    this.inModifica.set(null);
  }

  protected sezioneChiusa(nome: string): boolean {
    return this.chiuse().has(nome);
  }

  protected alternaSezione(nome: string): void {
    this.chiuse.update((s) => {
      const nuovo = new Set(s);
      if (!nuovo.delete(nome)) nuovo.add(nome);
      return nuovo;
    });
  }

  protected nonValido(campo: keyof typeof this.form.controls): boolean {
    const c = this.form.controls[campo];
    return c.invalid && c.touched;
  }

  protected salva(): void {
    const originale = this.inModifica();
    if (!originale) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const percentuale = v.tipoValore === 'Percentuale';
    const condizione: Condizione = {
      ...originale,
      ...v,
      categoria: v.categoria!,
      valore: v.valore ?? 0,
      // Per un tasso la periodicità è annua e il valore è sempre una percentuale.
      tipoValore: v.categoria === 'Tasso' ? 'Percentuale' : v.tipoValore,
      periodicita: v.categoria === 'Tasso' ? 'Annuale' : v.periodicita,
      importoMinimo: percentuale ? v.importoMinimo : null,
      importoMassimo: percentuale ? v.importoMassimo : null,
      dataFineValidita: v.dataFineValidita || null,
    };
    this.api.salvaCondizione(condizione).subscribe({
      next: (salvata) => {
        this.condizioni.update((elenco) => {
          const altre = elenco.filter((c) => c.id !== salvata.id);
          return [...altre, salvata].sort(
            (a, b) => a.categoria.localeCompare(b.categoria) || a.codice.localeCompare(b.codice),
          );
        });
        this.chiudi();
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

  /** Riassunto leggibile delle fasce di applicabilità. */
  protected fasce(c: Condizione): string {
    const intervallo = (da: number | null, a: number | null, unita: string) =>
      da === null && a === null ? null : `${da ?? 0}${a === null ? '+' : '–' + a} ${unita}`;
    return (
      [
        intervallo(c.fasciaImportoDa, c.fasciaImportoA, '€'),
        intervallo(c.fasciaDurataDaMesi, c.fasciaDurataAMesi, 'mesi'),
        intervallo(c.fasciaLtvDa, c.fasciaLtvA, '% LTV'),
      ]
        .filter((x) => x)
        .join(' · ') || 'Tutte'
    );
  }
}
