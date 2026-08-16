namespace Faturamento.Application.DTOs;

public sealed record ItemNotaFiscalDto(string CodigoProduto, int Quantidade);

public sealed record NotaFiscalDto(
    int Id,
    int Numero,
    string Status,
    DateTime DataCriacaoUtc,
    DateTime? DataFechamentoUtc,
    List<ItemNotaFiscalDto> Itens);
