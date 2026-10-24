import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Api, messaggioErrore } from '../core/api';
import { DecodificaPipe } from '../core/decodifica';
import { FINALITA_MUTUO, Finalita, Offerta, RataPiano, RisultatoSimulazione } from '../core/models';
import { Icona } from '../shared/icona';
import { Importo } from '../shared/importo';
import { Paginatore } from '../shared/paginatore';
import { StatoTabella, scaricaCsv } from '../shared/tabella';

type Scheda = 'riepilogo' | 'piano';

@Component({
  selector: 'app-simulatore',
  imports: [
    Importo,
    ReactiveFormsModule,
    CurrencyPipe,
    DecimalPipe,
    DatePipe,
    DecodificaPipe,
    Paginatore,
    Icona,
  ],
  templateUrl: './simulatore.html',
  styleUrl: './simulatore.scss',
})
export class Simulatore {
  private readonly api = inject(Api);
  private readonly fb = inject(FormBuilder);

  protected readonly tabelle = toSignal(this.api.tabelle$);

  protected readonly form = this.fb.nonNullable.group({
    finalita: ['AcquistoPrimaCasa' as Finalita, Validators.required],
    importo: [150_000, [Validators.required, Validators.min(1000)]],
    valoreImmobile: [250_000 as number | null],
    durataMesi: [300, Validators.required],
    provincia: ['' as string],
    etaRichiedente: [35, [Validators.required, Validators.min(18), Validators.max(99)]],
  });

  private readonly finalita = toSignal(this.form.controls.finalita.valueChanges, {
    initialValue: this.form.controls.finalita.value,
  });
  protected readonly mutuo = computed(() => FINALITA_MUTUO.includes(this.finalita()));

  /** Durate proposte: in anni per i mutui, in mesi per i prestiti. */
  protected readonly durate = computed(() =>
    this.mutuo()
      ? [5, 10, 15, 20, 25, 30].map((anni) => ({ mesi: anni * 12, etichetta: `${anni} anni` }))
      : [12, 24, 36, 48, 60, 72, 84, 96, 120].map((mesi) => ({ mesi, etichetta: `${mesi} mesi` })),
  );

  protected readonly caricamento = signal(false);
  protected readonly errore = signal<string | null>(null);
  protected readonly risultato = signal<RisultatoSimulazione | null>(null);
  protected readonly selezionata = signal<Offerta | null>(null);
  protected readonly scheda = signal<Scheda>('riepilogo');
  protected readonly mostraEsclusi = signal(false);

  protected readonly offerte = computed(() => this.risultato()?.offerte ?? []);
  protected readonly statoOfferte = new StatoTabella<Offerta>(this.offerte);
  protected readonly piano = computed(() => this.selezionata()?.pianoAmmortamento ?? []);
  protected readonly statoPiano = new StatoTabella<RataPiano>(this.piano);

  constructor() {
    // Al cambio di finalità si propone una durata coerente e si chiede il valore dell'immobile solo per i mutui.
    this.form.controls.finalita.valueChanges.subscribe((f) => {
      const mutuo = FINALITA_MUTUO.includes(f);
      const valore = this.form.controls.valoreImmobile;
      valore.setValidators(mutuo ? [Validators.required, Validators.min(1000)] : []);
      if (!mutuo) valore.setValue(null);
      valore.updateValueAndValidity();
      const durate = this.durate().map((d) => d.mesi);
      if (!durate.includes(this.form.controls.durataMesi.value)) {
        this.form.controls.durataMesi.setValue(mutuo ? 300 : 60);
      }
      if (!mutuo && this.form.controls.importo.value > 75_000)
        this.form.controls.importo.setValue(15_000);
    });
    this.form.controls.valoreImmobile.setValidators([Validators.required, Validators.min(1000)]);
  }

  protected calcola(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    this.caricamento.set(true);
    this.errore.set(null);
    this.api
      .simula({
        finalita: v.finalita,
        importo: v.importo,
        durataMesi: v.durataMesi,
        valoreImmobile: this.mutuo() ? v.valoreImmobile : null,
        provincia: v.provincia || null,
        etaRichiedente: v.etaRichiedente,
      })
      .subscribe({
        next: (r) => {
          this.risultato.set(r);
          this.statoOfferte.pagina.set(1);
          this.apri(r.offerte[0] ?? null);
          this.caricamento.set(false);
        },
        error: (e) => {
          this.errore.set(messaggioErrore(e));
          this.caricamento.set(false);
        },
      });
  }

  protected apri(offerta: Offerta | null): void {
    this.selezionata.set(offerta);
    this.statoPiano.pagina.set(1);
  }

  protected nonValido(campo: keyof typeof this.form.controls): boolean {
    const c = this.form.controls[campo];
    return c.invalid && c.touched;
  }

  protected scaricaPiano(): void {
    const o = this.selezionata();
    if (!o) return;
    scaricaCsv(
      `piano-ammortamento-${o.codice}.csv`,
      [
        'N. rata',
        'Scadenza',
        'Rata',
        'Quota capitale',
        'Quota interessi',
        'Spese',
        'Debito residuo',
      ],
      o.pianoAmmortamento.map((r) => [
        r.numero,
        r.dataScadenza,
        r.rata,
        r.quotaCapitale,
        r.quotaInteressi,
        r.spese,
        r.debitoResiduo,
      ]),
    );
  }
}
