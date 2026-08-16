import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { finalize } from 'rxjs';
import { NotaFiscalService } from '../../../core/services/nota-fiscal';
import { ProdutoService } from '../../../core/services/produto';
import { NotificationService } from '../../../core/services/notification';
import { Produto } from '../../../core/models/produto.model';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { LoadingButton } from '../../../shared/components/loading-button/loading-button';

@Component({
  selector: 'app-nota-form',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    PageHeader,
    LoadingButton,
  ],
  templateUrl: './nota-form.html',
  styleUrl: './nota-form.scss',
})
export class NotaForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly notaFiscalService = inject(NotaFiscalService);
  private readonly produtoService = inject(ProdutoService);
  private readonly notification = inject(NotificationService);
  private readonly router = inject(Router);

  protected readonly produtos = signal<Produto[]>([]);
  protected readonly salvando = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    itens: this.fb.array([this.criarLinhaItem()]),
  });

  get itens() {
    return this.form.controls.itens;
  }

  ngOnInit(): void {
    this.produtoService.listar().subscribe((produtos) => this.produtos.set(produtos));
  }

  private criarLinhaItem() {
    return this.fb.nonNullable.group({
      codigoProduto: ['', Validators.required],
      quantidade: [1, [Validators.required, Validators.min(1)]],
    });
  }

  protected adicionarItem(): void {
    this.itens.push(this.criarLinhaItem());
  }

  protected removerItem(index: number): void {
    if (this.itens.length > 1) {
      this.itens.removeAt(index);
    }
  }

  protected salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.salvando.set(true);

    this.notaFiscalService
      .criar(this.form.getRawValue())
      .pipe(finalize(() => this.salvando.set(false)))
      .subscribe({
        next: ({ id }) => {
          this.notification.sucesso('Nota fiscal criada com status Aberta.');
          this.router.navigate(['/notas-fiscais', id]);
        },
      });
  }

  protected cancelar(): void {
    this.router.navigate(['/notas-fiscais']);
  }
}
