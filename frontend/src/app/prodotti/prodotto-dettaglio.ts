import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Api, messaggioErrore } from '../core/api';
import { Condizione, Finalita, Prodotto, TipoTasso, TipologiaProdotto } from '../core/models';
import { CondizioniProdotto } from './condizioni-prodotto';
import { Importo } from '../shared/importo';

type Scheda = 'generali' | 'requisiti' | 'condizioni';

const oggi = () => new Date().toISOString().slice(0, 10);

@Component({
  selector: 'app-prodotto-dettaglio',
  imports: [Importo, ReactiveFormsModule, CondizioniProdotto],
  templateUrl: './prodotto-dettaglio.html',
})
export class ProdottoDettaglio {
  private readonly api = inject(Api);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  /** Parametro di rotta: assente per un nuovo prodotto. */
  readonly id = input<string>();

  protected readonly tabelle = toSignal(this.api.tabelle$);
  protected readonly indici = toSignal(this.api.indici(), { initialValue: [] });
  protected readonly scheda = signal<Scheda>('generali');
  protected readonly chiuse = signal<Set<string>>(new Set());
  protected readonly errore = signal<string | null>(null);
  protected readonly conferma = signal<string | null>(null);
  protected readonly salvataggio = signal(false);
  protected readonly condizioni = signal<Condizione[]>([]);
  protected readonly prodottoId = signal(0);
  protected readonly nuovo = computed(() => this.prodottoId() === 0);

  protected readonly form = this.fb.nonNullable.group({
    codice: ['', [Validators.required, Validators.maxLength(20)]],
    nome: ['', [Validators.required, Validators.maxLength(100)]],
    descrizione: [''],
    tipologia: ['Mutuo' as TipologiaProdotto, Validators.required],
    tipoTasso: ['Fisso' as TipoTasso, Validators.required],
    indiceRiferimento: [null as string | null],
    attivo: [true],
    dataInizioValidita: [oggi(), Validators.required],
    dataFineValidita: [null as string | null],
    finalitaAmmesse: [[] as Finalita[], Validators.required],
    importoMinimo: [1000, [Validators.required, Validators.min(0)]],
    importoMassimo: [100_000, [Validators.required, Validators.min(0)]],
    durataMinimaMesi: [12, [Validators.required, Validators.min(1), Validators.max(600)]],
    durataMassimaMesi: [120, [Validators.required, Validators.min(1), Validators.max(600)]],
    etaMinima: [18, [Validators.required, Validators.min(18)]],
    etaMassimaRichiedente: [null as number | null],
    etaMassimaFineFinanziamento: [null as number | null],
    ltvMassimo: [null as number | null, [Validators.min(0), Validators.max(100)]],
    provinceAmmesse: [''],
  });

  protected readonly tipoTasso = toSignal(this.form.controls.tipoTasso.valueChanges, {
    initialValue: this.form.controls.tipoTasso.value,
  });
  protected readonly tipologia = toSignal(this.form.controls.tipologia.valueChanges, {
    initialValue: this.form.controls.tipologia.value,
  });

  constructor() {
    effect(() => {
      const id = Number(this.id());
      if (id > 0) this.carica(id);
    });
  }

  private carica(id: number): void {
    this.api.prodotto(id).subscribe({
      next: (p) => {
        this.prodottoId.set(p.id);
        this.condizioni.set(p.condizioni);
        this.form.reset({
          ...p,
          descrizione: p.descrizione ?? '',
          provinceAmmesse: p.provinceAmmesse.join(', '),
        });
      },
      error: (e) => this.errore.set(messaggioErrore(e)),
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

  protected finalitaSelezionata(codice: string): boolean {
    return this.form.controls.finalitaAmmesse.value.includes(codice as Finalita);
  }

  protected alternaFinalita(codice: string, selezionata: boolean): void {
    const c = this.form.controls.finalitaAmmesse;
    const valori = c.value.filter((f) => f !== codice);
    c.setValue(selezionata ? [...valori, codice as Finalita] : valori);
    c.markAsTouched();
  }

  protected salva(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errore.set(
        'Controlla i campi obbligatori evidenziati in rosso, anche nelle altre schede.',
      );
      return;
    }
    const v = this.form.getRawValue();
    const prodotto: Prodotto = {
      ...v,
      id: this.prodottoId(),
      descrizione: v.descrizione || null,
      indiceRiferimento: v.tipoTasso === 'Variabile' ? v.indiceRiferimento : null,
      dataFineValidita: v.dataFineValidita || null,
      ltvMassimo: v.tipologia === 'Mutuo' ? v.ltvMassimo : null,
      provinceAmmesse: v.provinceAmmesse
        .split(/[\s,;]+/)
        .map((s) => s.trim().toUpperCase())
        .filter((s) => s.length > 0),
      condizioni: [],
    };
    this.salvataggio.set(true);
    this.errore.set(null);
    this.api.salvaProdotto(prodotto).subscribe({
      next: (salvato) => {
        this.salvataggio.set(false);
        this.conferma.set('Prodotto salvato correttamente.');
        // Dopo l'inserimento si passa al dettaglio, dove si abilitano le condizioni.
        if (this.nuovo()) this.router.navigate(['/prodotti', salvato.id], { replaceUrl: true });
        this.form.markAsPristine();
      },
      error: (e) => {
        this.salvataggio.set(false);
        this.errore.set(messaggioErrore(e));
      },
    });
  }

  protected annulla(): void {
    this.router.navigate(['/prodotti']);
  }
}
