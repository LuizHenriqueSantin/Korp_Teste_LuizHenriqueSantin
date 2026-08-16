import { Service, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiErrorItem } from '../models/api-error.model';

@Service()
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  sucesso(mensagem: string): void {
    this.snackBar.open(mensagem, 'Fechar', {
      duration: 4000,
      panelClass: 'snackbar-sucesso',
    });
  }

  erro(mensagem: string): void {
    this.snackBar.open(mensagem, 'Fechar', {
      duration: 6000,
      panelClass: 'snackbar-erro',
    });
  }

  errosDeApi(erros: ApiErrorItem[]): void {
    const mensagem = erros.map((e) => e.mensagem).join(' | ') || 'Ocorreu um erro inesperado.';
    this.erro(mensagem);
  }
}
