import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../services/notification';
import { ApiErrorResponse } from '../models/api-error.model';

export const errorHandlerInterceptor: HttpInterceptorFn = (req, next) => {
  const notification = inject(NotificationService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 400 && isApiErrorResponse(error.error)) {
        notification.errosDeApi(error.error.errors);
      } else if (error.status === 0) {
        notification.erro('Nao foi possivel se comunicar com o servidor. Tente novamente em instantes.');
      } else {
        notification.erro(`Erro inesperado (HTTP ${error.status}). Tente novamente.`);
      }

      return throwError(() => error);
    }),
  );
};

function isApiErrorResponse(body: unknown): body is ApiErrorResponse {
  return !!body && typeof body === 'object' && Array.isArray((body as ApiErrorResponse).errors);
}
