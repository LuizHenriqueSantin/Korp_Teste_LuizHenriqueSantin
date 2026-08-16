import { Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { ProdutoService } from '../../../core/services/produto';
import { Produto } from '../../../core/models/produto.model';
import { DataTable } from '../../../shared/components/data-table/data-table';
import { TableColumn } from '../../../shared/components/data-table/table-column.model';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { ProdutoForm } from '../produto-form/produto-form';

@Component({
  selector: 'app-produtos-list',
  imports: [MatButtonModule, MatIconModule, MatCardModule, DataTable, PageHeader],
  templateUrl: './produtos-list.html',
  styleUrl: './produtos-list.scss',
})
export class ProdutosList implements OnInit {
  private readonly produtoService = inject(ProdutoService);
  private readonly dialog = inject(MatDialog);

  protected readonly produtos = signal<Produto[]>([]);

  protected readonly colunas: TableColumn<Produto>[] = [
    { key: 'codigo', label: 'Codigo' },
    { key: 'descricao', label: 'Descricao' },
    { key: 'saldo', label: 'Saldo' },
  ];

  ngOnInit(): void {
    this.carregar();
  }

  private carregar(): void {
    this.produtoService.listar().subscribe((produtos) => this.produtos.set(produtos));
  }

  protected abrirFormulario(): void {
    this.dialog
      .open(ProdutoForm)
      .afterClosed()
      .subscribe((criado) => {
        if (criado) {
          this.carregar();
        }
      });
  }
}
