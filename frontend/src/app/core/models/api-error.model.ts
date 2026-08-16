export interface ApiErrorItem {
  chave: string;
  mensagem: string;
}

export interface ApiErrorResponse {
  errors: ApiErrorItem[];
}
