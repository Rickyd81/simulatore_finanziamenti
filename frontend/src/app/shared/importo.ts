import { Directive, ElementRef, forwardRef, inject } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

/** Cifre e virgola: gli unici caratteri che contano per il valore digitato. */
const SIGNIFICATIVO = /[\d,]/;

/**
 * Campo importo in formato italiano: mostra i punti delle migliaia mentre si digita
 * (es. 150.000 o 1.250,50) e passa al form il valore numerico.
 */
@Directive({
  selector: 'input[appImporto]',
  host: {
    type: 'text',
    inputmode: 'decimal',
    autocomplete: 'off',
    class: 'importo',
    '(input)': 'digitato()',
    '(blur)': 'uscita()',
  },
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => Importo), multi: true }],
})
export class Importo implements ControlValueAccessor {
  private readonly input = inject<ElementRef<HTMLInputElement>>(ElementRef).nativeElement;
  private onChange: (valore: number | null) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(valore: number | null): void {
    this.input.value = formattaImporto(valore);
  }

  registerOnChange(fn: (valore: number | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(disabilitato: boolean): void {
    this.input.disabled = disabilitato;
  }

  protected digitato(): void {
    const testo = this.input.value;
    const cursore = this.input.selectionStart ?? testo.length;
    const significativiPrima = [...testo.slice(0, cursore)].filter((c) =>
      SIGNIFICATIVO.test(c),
    ).length;

    const formattato = formattaDigitato(testo);
    this.input.value = formattato;

    // Riporta il cursore dopo lo stesso numero di cifre, ignorando i punti aggiunti o tolti.
    let posizione = 0;
    for (let visti = 0; posizione < formattato.length && visti < significativiPrima; posizione++) {
      if (SIGNIFICATIVO.test(formattato[posizione])) visti++;
    }
    this.input.setSelectionRange(posizione, posizione);

    this.onChange(leggiImporto(formattato));
  }

  protected uscita(): void {
    this.input.value = formattaImporto(leggiImporto(this.input.value));
    this.onTouched();
  }
}

/** 1250.5 → "1.250,5"; null → "". */
export function formattaImporto(valore: number | null | undefined): string {
  if (valore === null || valore === undefined || Number.isNaN(valore)) return '';
  // Non si usa toLocaleString: in italiano Intl non raggruppa sotto 10.000 (1250), qui serve 1.250.
  return formattaDigitato(String(Math.round(valore * 100) / 100).replace('.', ','));
}

/** "1.250,50" → 1250.5; testo vuoto → null. */
export function leggiImporto(testo: string): number | null {
  const pulito = testo.replace(/\./g, '').replace(',', '.').trim();
  if (!pulito || pulito === '.') return null;
  const valore = Number(pulito);
  return Number.isFinite(valore) ? valore : null;
}

/** Riformatta il testo mentre si digita, lasciando la virgola e i decimali come scritti. */
export function formattaDigitato(testo: string): string {
  const [intero, ...resto] = testo.replace(/[^\d,]/g, '').split(',');
  const cifre = intero.replace(/^0+(?=\d)/, '');
  const conPunti = cifre.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  return resto.length ? `${conPunti},${resto.join('').slice(0, 2)}` : conPunti;
}
