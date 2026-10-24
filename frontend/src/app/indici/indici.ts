import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Api, messaggioErrore } from '../core/api';
import { IndiceRiferimento } from '../core/models';
import { Icona } from '../shared/icona';

/** Valori degli indici di mercato usati dai prodotti a tasso variabile. */
@Component({
  selector: 'app-indici',
  imports: [ReactiveFormsModule, DecimalPipe, DatePipe, Icona],
  templateUrl: './indici.html',
})
export class Indici {
  private readonly api = inject(Api);
  private readonly fb = inject(FormBuilder);

  protected readonly indici = signal<IndiceRiferimento[]>([]);
  protected readonly errore = signal<string | null>(null);
  protected readonly inModifica = signal<IndiceRiferimento | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    codice: ['', [Validators.required, Validators.maxLength(20)]],
    descrizione: ['', Validators.required],
    valore: [0, Validators.required],
    dataRilevazione: [new Date().toISOString().slice(0, 10), Validators.required],
  });

  constructor() {
    this.carica();
  }

  private carica(): void {
    this.api.indici().subscribe({
      next: (i) => this.indici.set(i),
      error: (e) => this.errore.set(messaggioErrore(e)),
    });
  }

  protected apri(indice: IndiceRiferimento | null): void {
    const nuovo = {
      codice: '',
      descrizione: '',
      valore: 0,
      dataRilevazione: new Date().toISOString().slice(0, 10),
    };
    this.inModifica.set(indice ?? nuovo);
    this.form.reset(indice ?? nuovo);
    if (indice) this.form.controls.codice.disable();
    else this.form.controls.codice.enable();
  }

  protected salva(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.api.salvaIndice(this.form.getRawValue()).subscribe({
      next: () => {
        this.inModifica.set(null);
        this.carica();
      },
      error: (e) => this.errore.set(messaggioErrore(e)),
    });
  }

  protected elimina(indice: IndiceRiferimento): void {
    if (
      !confirm(
        `Eliminare l'indice ${indice.codice}? I prodotti che lo usano non saranno più simulabili.`,
      )
    )
      return;
    this.api.eliminaIndice(indice.codice).subscribe({
      next: () => this.carica(),
      error: (e) => this.errore.set(messaggioErrore(e)),
    });
  }
}
