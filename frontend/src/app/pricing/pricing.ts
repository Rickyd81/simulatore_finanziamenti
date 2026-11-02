import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Api, messaggioErrore } from '../core/api';
import { Pricing } from '../core/models';
import { Icona } from '../shared/icona';

/**
 * Anagrafica pricing: valore degli indici di mercato usati dai prodotti a tasso variabile e
 * spread proposto. Il prodotto che richiama un pricing ne importa lo spread come copia.
 */
@Component({
  selector: 'app-anagrafica-pricing',
  imports: [ReactiveFormsModule, DecimalPipe, DatePipe, Icona],
  templateUrl: './pricing.html',
})
export class AnagraficaPricing {
  private readonly api = inject(Api);
  private readonly fb = inject(FormBuilder);

  protected readonly elenco = signal<Pricing[]>([]);
  protected readonly errore = signal<string | null>(null);
  protected readonly inModifica = signal<Pricing | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    codice: ['', [Validators.required, Validators.maxLength(20)]],
    descrizione: ['', Validators.required],
    valore: [0, Validators.required],
    spread: [null as number | null, Validators.required],
    dataRilevazione: [new Date().toISOString().slice(0, 10), Validators.required],
  });

  constructor() {
    this.carica();
  }

  private carica(): void {
    this.api.pricing().subscribe({
      next: (p) => this.elenco.set(p),
      error: (e) => this.errore.set(messaggioErrore(e)),
    });
  }

  protected apri(pricing: Pricing | null): void {
    const nuovo: Pricing = {
      codice: '',
      descrizione: '',
      valore: 0,
      spread: null,
      dataRilevazione: new Date().toISOString().slice(0, 10),
    };
    this.inModifica.set(pricing ?? nuovo);
    this.form.reset(pricing ?? nuovo);
    if (pricing) this.form.controls.codice.disable();
    else this.form.controls.codice.enable();
  }

  protected salva(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.api.salvaPricing(this.form.getRawValue()).subscribe({
      next: () => {
        this.inModifica.set(null);
        this.carica();
      },
      error: (e) => this.errore.set(messaggioErrore(e)),
    });
  }

  protected elimina(pricing: Pricing): void {
    if (
      !confirm(
        `Eliminare il pricing ${pricing.codice}? I prodotti che lo usano non saranno più simulabili.`,
      )
    )
      return;
    this.api.eliminaPricing(pricing.codice).subscribe({
      next: () => this.carica(),
      error: (e) => this.errore.set(messaggioErrore(e)),
    });
  }
}
