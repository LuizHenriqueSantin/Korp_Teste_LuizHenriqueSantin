import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, Validators, FormBuilder } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { finalize } from 'rxjs';
import { ProdutoService } from '../../../core/services/produto';
import { NotificationService } from '../../../core/services/notification';
import { LoadingButton } from '../../../shared/components/loading-button/loading-button';

@Component({
  selector: 'app-produto-form',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    LoadingButton,
  ],
  templateUrl: './produto-form.html',
  styleUrl: './produto-form.scss',
})
export class ProdutoForm {
  private readonly fb = inject(FormBuilder);
  private readonly produtoService = inject(ProdutoService);
  private readonly notification = inject(NotificationService);
  private readonly dialogRef = inject(MatDialogRef<ProdutoForm>);

  protected readonly salvando = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    codigo: ['', [Validators.required, Validators.maxLength(30)]],
    descricao: ['', [Validators.required, Validators.maxLength(200)]],
    saldoInicial: [0, [Validators.required, Validators.min(0)]],
  });

  protected salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.salvando.set(true);

    this.produtoService
      .criar(this.form.getRawValue())
      .pipe(finalize(() => this.salvando.set(false)))
      .subscribe({
        next: () => {
          this.notification.sucesso('Produto cadastrado com sucesso.');
          this.dialogRef.close(true);
        },
      });
  }

  protected cancelar(): void {
    this.dialogRef.close(false);
  }
}
