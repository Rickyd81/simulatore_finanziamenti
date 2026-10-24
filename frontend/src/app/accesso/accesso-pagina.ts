import { Component, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { messaggioErrore } from '../core/api';
import { Accesso } from '../core/accesso';

@Component({
  selector: 'app-accesso-pagina',
  imports: [FormsModule],
  template: `
    <div class="pagina">
      <form class="accesso" (ngSubmit)="entra()">
        <div class="sezione">
          <div class="sezione-titolo">Accesso all'area banca</div>
          <div class="sezione-corpo">
            <p class="tenue">
              La fabbrica prodotti e gli indici di riferimento sono riservati agli utenti della
              banca.
            </p>
            @if (errore(); as e) {
              <p class="messaggio errore">{{ e }}</p>
            }
            <div class="campo">
              <label for="password">Password *</label>
              <input
                id="password"
                type="password"
                name="password"
                autocomplete="current-password"
                [(ngModel)]="password"
                required
              />
            </div>
          </div>
        </div>
        <div class="azioni-fondo">
          <button type="submit" class="btn btn-scuro" [disabled]="!password || invio()">
            Accedi
          </button>
        </div>
      </form>
    </div>
  `,
  styles: `
    .accesso {
      max-width: 420px;
    }
  `,
})
export class AccessoPagina {
  private readonly accesso = inject(Accesso);
  private readonly router = inject(Router);

  /** Pagina da riaprire dopo l'accesso (query string). */
  readonly ritorno = input<string>();

  protected password = '';
  protected readonly errore = signal<string | null>(null);
  protected readonly invio = signal(false);

  protected entra(): void {
    this.invio.set(true);
    this.errore.set(null);
    this.accesso.entra(this.password).subscribe({
      next: () => this.router.navigateByUrl(this.ritorno() || '/prodotti'),
      error: (e) => {
        this.invio.set(false);
        this.errore.set(messaggioErrore(e));
      },
    });
  }
}
