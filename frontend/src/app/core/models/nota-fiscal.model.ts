export type StatusNotaFiscal = 'Aberta' | 'Fechada';

export interface ItemNotaFiscal {
  codigoProduto: string;
  quantidade: number;
}

export interface NotaFiscal {
  id: number;
  numero: number;
  status: StatusNotaFiscal;
  dataCriacaoUtc: string;
  dataFechamentoUtc: string | null;
  itens: ItemNotaFiscal[];
}

export interface ItemRequest {
  codigoProduto: string;
  quantidade: number;
}

export interface CriarNotaFiscalRequest {
  itens: ItemRequest[];
}
