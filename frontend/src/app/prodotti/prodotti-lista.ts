import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Api, messaggioErrore } from '../core/api';
import { DecodificaPipe } from '../core/decodifica';
import { Prodotto } from '../core/models';
import { Icona } from '../shared/icona';
import { Paginatore } from '../shared/paginatore';
import { StatoTabella, scaricaCsv } from '../shared/tabella';

@Component({
  selector: 'app-prodotti-lista',
  imports: [FormsModule, DecimalPipe, DecodificaPipe, Paginatore, Icona],
  templateUrl: './prodotti-lista.html',
})
export class ProdottiLista {
  private readonly api = inject(Api);
  private readonly router = inject(Router);

  protected readonly prodotti = signal<Prodotto[]>([]);
  protected readonly filtro = signal('');
  protected readonly errore = signal<string | null>(null);

  protected readonly filtrati = computed(() => {
    const testo = this.filtro().trim().toLowerCase();
    if (!testo) return this.prodotti();
    return this.prodotti().filter((p) => `${p.codice} ${p.nome}`.toLowerCase().includes(testo));
  });
  protected readonly stato = new StatoTabella<Prodotto>(this.filtrati);
  protected readonly attivi = computed(() => this.prodotti().filter((p) => p.attivo).length);

  constructor() {
    this.carica();
  }

  private carica(): void {
    this.api.prodotti().subscribe({
      next: (p) => this.prodotti.set(p),
      error: (e) => this.errore.set(messaggioErrore(e)),
    });
  }

  protected apri(prodotto: Prodotto | null): void {
    this.router.navigate(['/prodotti', prodotto?.id ?? 'nuovo']);
  }

  protected elimina(prodotto: Prodotto): void {
    if (!confirm(`Eliminare il prodotto ${prodotto.codice} e tutte le sue condizioni?`)) return;
    this.api.eliminaProdotto(prodotto.id).subscribe({
      next: () => this.carica(),
      error: (e) => this.errore.set(messaggioErrore(e)),
    });
  }

  protected scarica(): void {
    scaricaCsv(
      'prodotti.csv',
      [
        'Codice',
        'Nome',
        'Tipologia',
        'Tasso',
        'Importo minimo',
        'Importo massimo',
        'Durata minima (mesi)',
        'Durata massima (mesi)',
        'Attivo',
      ],
      this.stato
        .ordinate()
        .map((p) => [
          p.codice,
          p.nome,
          p.tipologia,
          p.tipoTasso,
          p.importoMinimo,
          p.importoMassimo,
          p.durataMinimaMesi,
          p.durataMassimaMesi,
          p.attivo ? 'Sì' : 'No',
        ]),
    );
  }
}
