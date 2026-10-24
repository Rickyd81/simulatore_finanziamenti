import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'simulatore' },
  {
    path: 'simulatore',
    data: { titolo: 'Simulatore finanziamenti' },
    loadComponent: () => import('./simulatore/simulatore').then((m) => m.Simulatore),
  },
  {
    path: 'prodotti',
    data: { titolo: 'Fabbrica prodotti' },
    loadComponent: () => import('./prodotti/prodotti-lista').then((m) => m.ProdottiLista),
  },
  {
    path: 'prodotti/nuovo',
    data: { titolo: 'Nuovo prodotto' },
    loadComponent: () => import('./prodotti/prodotto-dettaglio').then((m) => m.ProdottoDettaglio),
  },
  {
    path: 'prodotti/:id',
    data: { titolo: 'Dettaglio prodotto' },
    loadComponent: () => import('./prodotti/prodotto-dettaglio').then((m) => m.ProdottoDettaglio),
  },
  {
    path: 'indici',
    data: { titolo: 'Indici di riferimento' },
    loadComponent: () => import('./indici/indici').then((m) => m.Indici),
  },
  { path: '**', redirectTo: 'simulatore' },
];
