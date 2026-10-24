import { Routes } from '@angular/router';
import { areaBancaGuard } from './core/accesso';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'simulatore' },
  {
    path: 'simulatore',
    data: { titolo: 'Simulatore finanziamenti' },
    loadComponent: () => import('./simulatore/simulatore').then((m) => m.Simulatore),
  },
  {
    path: 'prodotti',
    canActivate: [areaBancaGuard],
    data: { titolo: 'Fabbrica prodotti' },
    loadComponent: () => import('./prodotti/prodotti-lista').then((m) => m.ProdottiLista),
  },
  {
    path: 'prodotti/nuovo',
    canActivate: [areaBancaGuard],
    data: { titolo: 'Nuovo prodotto' },
    loadComponent: () => import('./prodotti/prodotto-dettaglio').then((m) => m.ProdottoDettaglio),
  },
  {
    path: 'prodotti/:id',
    canActivate: [areaBancaGuard],
    data: { titolo: 'Dettaglio prodotto' },
    loadComponent: () => import('./prodotti/prodotto-dettaglio').then((m) => m.ProdottoDettaglio),
  },
  {
    path: 'indici',
    canActivate: [areaBancaGuard],
    data: { titolo: 'Indici di riferimento' },
    loadComponent: () => import('./indici/indici').then((m) => m.Indici),
  },
  {
    path: 'accesso',
    data: { titolo: 'Accesso area banca' },
    loadComponent: () => import('./accesso/accesso-pagina').then((m) => m.AccessoPagina),
  },
  { path: '**', redirectTo: 'simulatore' },
];
