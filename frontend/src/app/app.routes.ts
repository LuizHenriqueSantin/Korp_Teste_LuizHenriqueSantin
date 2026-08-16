import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'produtos' },
  {
    path: 'produtos',
    loadComponent: () =>
      import('./features/produtos/produtos-list/produtos-list').then((m) => m.ProdutosList),
  },
  {
    path: 'notas-fiscais',
    loadComponent: () =>
      import('./features/notas-fiscais/notas-list/notas-list').then((m) => m.NotasList),
  },
  {
    path: 'notas-fiscais/nova',
    loadComponent: () =>
      import('./features/notas-fiscais/nota-form/nota-form').then((m) => m.NotaForm),
  },
  {
    path: 'notas-fiscais/:id',
    loadComponent: () =>
      import('./features/notas-fiscais/nota-detalhe/nota-detalhe').then((m) => m.NotaDetalhe),
  },
];
