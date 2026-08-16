import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { filter, finalize, map, switchMap, tap } from 'rxjs';
import { NotaFiscalService } from '../../../core/services/nota-fiscal';
import { NotificationService } from '../../../core/services/notification';
import { NotaFiscal } from '../../../core/models/nota-fiscal.model';
import { DataTable } from '../../../shared/components/data-table/data-table';
import { TableColumn } from '../../../shared/components/data-table/table-column.model';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { LoadingButton } from '../../../shared/components/loading-button/loading-button';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/components/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-nota-detalhe',
  imports: [
    DatePipe,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatCardModule,
    DataTable,
    PageHeader,
    LoadingButton,
  ],
  templateUrl: './nota-detalhe.html',
  styleUrl: './nota-detalhe.scss',
})
export class NotaDetalhe implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly notaFiscalService = inject(NotaFiscalService);
  private readonly notification = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  protected readonly nota = signal<NotaFiscal | null>(null);
  protected readonly imprimindo = signal(false);

  protected readonly colunasItens: TableColumn<{ codigoProduto: string; quantidade: number }>[] = [
    { key: 'codigoProduto', label: 'Codigo do produto' },
    { key: 'quantidade', label: 'Quantidade' },
  ];

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        map((params) => Number(params.get('id'))),
        switchMap((id) => this.notaFiscalService.obterPorId(id)),
      )
      .subscribe((nota) => this.nota.set(nota));
  }

  protected imprimir(): void {
    const notaAtual = this.nota();
    if (!notaAtual || notaAtual.status !== 'Aberta') {
      return;
    }

    const dialogData: ConfirmDialogData = {
      title: 'Imprimir nota fiscal',
      message:
        'Essa acao ira debitar o saldo dos produtos no estoque e fechar a nota definitivamente. Deseja continuar?',
      confirmLabel: 'Imprimir',
    };

    this.dialog
      .open(ConfirmDialog, { data: dialogData })
      .afterClosed()
      .pipe(
        filter((confirmado) => !!confirmado),
        tap(() => this.imprimindo.set(true)),
        switchMap(() => this.notaFiscalService.imprimir(notaAtual.id)),
        switchMap(() => this.notaFiscalService.obterPorId(notaAtual.id)),
        finalize(() => this.imprimindo.set(false)),
      )
      .subscribe({
        next: (notaAtualizada) => {
          this.nota.set(notaAtualizada);
          this.notification.sucesso('Nota impressa: saldo debitado e nota fechada.');
          setTimeout(() => window.print(), 300);
        },
      });
  }

  protected voltar(): void {
    this.router.navigate(['/notas-fiscais']);
  }
}
