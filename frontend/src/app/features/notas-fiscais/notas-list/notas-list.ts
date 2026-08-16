import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatCardModule } from '@angular/material/card';
import { NotaFiscalService } from '../../../core/services/nota-fiscal';
import { NotaFiscal } from '../../../core/models/nota-fiscal.model';
import { DataTable } from '../../../shared/components/data-table/data-table';
import { TableColumn } from '../../../shared/components/data-table/table-column.model';
import { PageHeader } from '../../../shared/components/page-header/page-header';

@Component({
  selector: 'app-notas-list',
  imports: [MatButtonModule, MatIconModule, MatChipsModule, MatCardModule, DataTable, PageHeader],
  templateUrl: './notas-list.html',
  styleUrl: './notas-list.scss',
})
export class NotasList implements OnInit {
  private readonly notaFiscalService = inject(NotaFiscalService);
  private readonly router = inject(Router);

  protected readonly notas = signal<NotaFiscal[]>([]);

  protected readonly colunas: TableColumn<NotaFiscal>[] = [
    { key: 'numero', label: 'Numero', format: (n) => `#${n.numero}` },
    { key: 'status', label: 'Status', format: (n) => (n.status === 'Aberta' ? '🟢 Aberta' : '⚪ Fechada') },
    { key: 'itens', label: 'Itens', format: (n) => `${n.itens.length} produto(s)` },
    {
      key: 'dataCriacaoUtc',
      label: 'Criada em',
      format: (n) => new Date(n.dataCriacaoUtc).toLocaleString('pt-BR'),
    },
  ];

  ngOnInit(): void {
    this.carregar();
  }

  private carregar(): void {
    this.notaFiscalService.listar().subscribe((notas) => this.notas.set(notas));
  }

  protected novaNota(): void {
    this.router.navigate(['/notas-fiscais/nova']);
  }

  protected abrirDetalhe(nota: NotaFiscal): void {
    this.router.navigate(['/notas-fiscais', nota.id]);
  }
}
