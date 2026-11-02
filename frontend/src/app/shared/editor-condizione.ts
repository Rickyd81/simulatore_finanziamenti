import {
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Api } from '../core/api';
import {
  CategoriaCondizione,
  DatiCondizione,
  Finalita,
  Periodicita,
  TipoTasso,
  TipoValore,
} from '../core/models';
import { DecimalPipe } from '@angular/common';
import { Importo } from './importo';

/** Condizione in modifica: i dati economici più l'eventuale gruppo dell'anagrafica. */
export type CondizioneEditabile = DatiCondizione & { gruppo?: string | null };

/** Condizione vuota da cui partire per un nuovo inserimento. */
export function condizioneVuota(): DatiCondizione {
  return {
    id: 0,
    codice: '',
    categoria: null as unknown as CategoriaCondizione,
    descrizione: '',
    tipoValore: 'Percentuale',
    valore: 0,
    codicePricing: null,
    tassoRiferimento: null,
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
  };
}

/**
 * Dettaglio di una condizione economica, usato sia dal condizioniere del prodotto
 * sia dall'anagrafica condizioni. Il salvataggio lo fa il componente che lo ospita.
 */
@Component({
  selector: 'app-editor-condizione',
  imports: [DecimalPipe, Importo, ReactiveFormsModule],
  templateUrl: './editor-condizione.html',
})
export class EditorCondizione {
  private readonly fb = inject(FormBuilder);

  readonly condizione = input.required<CondizioneEditabile>();
  /** Tipo di tasso del prodotto, per proporre solo i suoi pricing; null nell'anagrafica (tutti i pricing). */
  readonly tipoTasso = input<TipoTasso | null>(null);
  readonly conGruppo = input(false);
  readonly gruppi = input<string[]>([]);
  readonly nota = input<string | null>(null);

  readonly salva = output<CondizioneEditabile>();
  readonly annulla = output<void>();

  private readonly api = inject(Api);
  protected readonly tabelle = toSignal(this.api.tabelle$);
  private readonly pricing = toSignal(this.api.pricing(), { initialValue: [] });
  protected readonly pricingProponibili = computed(() => {
    const tipo = this.tipoTasso();
    return tipo ? this.pricing().filter((p) => p.tipoTasso === tipo) : this.pricing();
  });
  protected readonly chiuse = signal<Set<string>>(new Set());

  protected readonly form = this.fb.group({
    codice: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(20)]),
    gruppo: this.fb.control<string | null>(null, Validators.maxLength(100)),
    categoria: this.fb.control<CategoriaCondizione | null>(null, Validators.required),
    descrizione: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(200)]),
    tipoValore: this.fb.nonNullable.control<TipoValore>('Percentuale'),
    valore: this.fb.control<number | null>(null, Validators.required),
    codicePricing: this.fb.control<string | null>(null),
    tassoRiferimento: this.fb.control<number | null>(null),
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
  private readonly tassoRiferimento = toSignal(this.form.controls.tassoRiferimento.valueChanges);
  private readonly spread = toSignal(this.form.controls.valore.valueChanges);
  /** TAN della condizione di tasso mentre la si compila: tasso di riferimento + spread. */
  protected readonly tan = computed(() => (this.tassoRiferimento() ?? 0) + (this.spread() ?? 0));

  constructor() {
    effect(() => {
      const condizione = this.condizione();
      untracked(() => {
        this.form.reset({ gruppo: null, ...condizione });
        // Il codice di una condizione esistente non si modifica, come nella schermata di riferimento.
        if (condizione.id) this.form.controls.codice.disable();
        else this.form.controls.codice.enable();
      });
    });
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

  /** Richiamando un pricing se ne importano tasso di riferimento e spread, che restano modificabili. */
  protected importaPricing(): void {
    const pricing = this.pricing().find((p) => p.codice === this.form.controls.codicePricing.value);
    if (!pricing) return;
    this.form.controls.tassoRiferimento.setValue(pricing.valore);
    this.form.controls.valore.setValue(pricing.spread);
  }

  protected conferma(): void {
    // Pricing e tasso di riferimento sono obbligatori solo per le condizioni di tasso.
    const tasso = this.form.controls.categoria.value === 'Tasso';
    for (const c of [this.form.controls.codicePricing, this.form.controls.tassoRiferimento]) {
      c.setValidators(tasso ? Validators.required : null);
      c.updateValueAndValidity();
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { gruppo, ...v } = this.form.getRawValue();
    const percentuale = v.tipoValore === 'Percentuale';
    this.salva.emit({
      ...this.condizione(),
      ...v,
      ...(this.conGruppo() ? { gruppo: gruppo?.trim() || null } : {}),
      categoria: v.categoria!,
      valore: v.valore ?? 0,
      // Per un tasso la periodicità è annua e il valore è sempre una percentuale.
      tipoValore: v.categoria === 'Tasso' ? 'Percentuale' : v.tipoValore,
      periodicita: v.categoria === 'Tasso' ? 'Annuale' : v.periodicita,
      codicePricing: tasso ? v.codicePricing : null,
      tassoRiferimento: tasso ? v.tassoRiferimento : null,
      importoMinimo: percentuale ? v.importoMinimo : null,
      importoMassimo: percentuale ? v.importoMassimo : null,
      dataFineValidita: v.dataFineValidita || null,
    });
  }
}

/** Riassunto leggibile delle fasce di applicabilità di una condizione. */
export function descriviFasce(c: DatiCondizione): string {
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
