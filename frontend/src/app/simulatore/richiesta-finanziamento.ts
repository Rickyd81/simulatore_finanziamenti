import { CurrencyPipe, DecimalPipe } from '@angular/common';
import {
  Component,
  ElementRef,
  afterNextRender,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Api, messaggioErrore } from '../core/api';
import { EsitoRichiestaFinanziamento, Offerta, RichiestaSimulazione } from '../core/models';

/** Stessa regola del back-end: 16 caratteri, con le lettere di omocodia ammesse al posto delle cifre. */
const CODICE_FISCALE =
  /^[A-Za-z]{6}[0-9LMNP-Vlmnp-v]{2}[A-Za-z][0-9LMNP-Vlmnp-v]{2}[A-Za-z][0-9LMNP-Vlmnp-v]{3}[A-Za-z]$/;

/**
 * Pop-up per richiedere il finanziamento dell'offerta scelta: raccoglie i riferimenti del cliente
 * e li invia, con l'offerta, al core banking tramite l'API standard.
 */
@Component({
  selector: 'app-richiesta-finanziamento',
  imports: [ReactiveFormsModule, CurrencyPipe, DecimalPipe],
  templateUrl: './richiesta-finanziamento.html',
  styleUrl: './richiesta-finanziamento.scss',
})
export class RichiestaFinanziamentoDialog {
  private readonly api = inject(Api);
  private readonly fb = inject(FormBuilder);

  readonly offerta = input.required<Offerta>();
  readonly simulazione = input.required<RichiestaSimulazione>();
  readonly chiuso = output<void>();

  private readonly dialogo = viewChild.required<ElementRef<HTMLDialogElement>>('dialogo');

  protected readonly form = this.fb.nonNullable.group({
    nome: ['', [Validators.required, Validators.maxLength(60)]],
    cognome: ['', [Validators.required, Validators.maxLength(60)]],
    codiceFiscale: ['', [Validators.required, Validators.pattern(CODICE_FISCALE)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(120)]],
    telefono: ['', Validators.pattern(/^\+?[0-9 ]{6,20}$/)],
    consensoPrivacy: [false, Validators.requiredTrue],
  });

  protected readonly invio = signal(false);
  protected readonly errore = signal<string | null>(null);
  protected readonly esito = signal<EsitoRichiestaFinanziamento | null>(null);
  protected readonly mostraJson = signal(false);
  protected readonly copiato = signal(false);

  constructor() {
    afterNextRender(() => this.dialogo().nativeElement.showModal());
  }

  protected nonValido(campo: keyof typeof this.form.controls): boolean {
    const c = this.form.controls[campo];
    return c.invalid && c.touched;
  }

  protected invia(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    this.invio.set(true);
    this.errore.set(null);
    this.api
      .inviaRichiestaFinanziamento({
        prodottoId: this.offerta().prodottoId,
        simulazione: this.simulazione(),
        cliente: { ...v, telefono: v.telefono || null },
      })
      .subscribe({
        next: (esito) => {
          this.esito.set(esito);
          this.invio.set(false);
        },
        error: (e) => {
          this.errore.set(messaggioErrore(e));
          this.invio.set(false);
        },
      });
  }

  protected json(): string {
    return JSON.stringify(this.esito()?.pratica, null, 2);
  }

  protected copiaJson(): void {
    navigator.clipboard?.writeText(this.json()).then(() => this.copiato.set(true));
  }

  protected chiudi(): void {
    this.dialogo().nativeElement.close();
  }
}
