import { Signal, computed, signal } from '@angular/core';

export type Direzione = 'asc' | 'desc';

/** Ordinamento e paginazione lato client per le tabelle in stile lista. */
export class StatoTabella<T> {
  readonly colonna = signal<keyof T | null>(null);
  readonly direzione = signal<Direzione>('asc');
  readonly pagina = signal(1);
  readonly dimensione = signal(10);

  readonly ordinate = computed(() => {
    const righe = [...this.righe()];
    const colonna = this.colonna();
    if (!colonna) return righe;
    const segno = this.direzione() === 'asc' ? 1 : -1;
    return righe.sort((a, b) => {
      const x = a[colonna] as unknown;
      const y = b[colonna] as unknown;
      if (typeof x === 'number' && typeof y === 'number') return (x - y) * segno;
      return String(x ?? '').localeCompare(String(y ?? ''), 'it') * segno;
    });
  });

  readonly totale = computed(() => this.righe().length);
  readonly pagine = computed(() => Math.max(1, Math.ceil(this.totale() / this.dimensione())));
  readonly visibili = computed(() => {
    const inizio = (Math.min(this.pagina(), this.pagine()) - 1) * this.dimensione();
    return this.ordinate().slice(inizio, inizio + this.dimensione());
  });

  constructor(private readonly righe: Signal<T[]>) {}

  ordina(colonna: keyof T): void {
    if (this.colonna() === colonna) {
      this.direzione.update((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      this.colonna.set(colonna);
      this.direzione.set('asc');
    }
  }

  icona(colonna: keyof T): string {
    if (this.colonna() !== colonna) return '⇅';
    return this.direzione() === 'asc' ? '↑' : '↓';
  }
}

/** Esporta righe in CSV (separatore ";" e virgola decimale, come si aspetta Excel in italiano). */
export function scaricaCsv(
  nomeFile: string,
  intestazioni: string[],
  righe: (string | number | null)[][],
): void {
  const cella = (v: string | number | null) => {
    const testo =
      typeof v === 'number'
        ? v.toLocaleString('it-IT', { useGrouping: false, maximumFractionDigits: 6 })
        : (v ?? '');
    return /[";\n]/.test(testo) ? `"${testo.replace(/"/g, '""')}"` : testo;
  };
  const csv = [intestazioni, ...righe].map((r) => r.map(cella).join(';')).join('\r\n');
  const url = URL.createObjectURL(new Blob(['\ufeff' + csv], { type: 'text/csv;charset=utf-8' }));
  const link = document.createElement('a');
  link.href = url;
  link.download = nomeFile;
  link.click();
  URL.revokeObjectURL(url);
}
