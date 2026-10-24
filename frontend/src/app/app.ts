import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  ActivatedRoute,
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { filter, map } from 'rxjs';

interface VoceMenu {
  percorso: string;
  etichetta: string;
}

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly portale: VoceMenu[] = [{ percorso: '/simulatore', etichetta: 'Simulatore' }];
  protected readonly banca: VoceMenu[] = [
    { percorso: '/prodotti', etichetta: 'Fabbrica prodotti' },
    { percorso: '/indici', etichetta: 'Indici di riferimento' },
  ];

  /** Il titolo della barra in alto è quello della rotta attiva più interna. */
  protected readonly titolo = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationEnd),
      map(() => {
        let r = this.route.snapshot;
        while (r.firstChild) r = r.firstChild;
        return (r.data['titolo'] as string | undefined) ?? '';
      }),
    ),
    { initialValue: '' },
  );
}
