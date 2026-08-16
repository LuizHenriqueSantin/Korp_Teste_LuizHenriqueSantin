namespace Estoque.Application.DTOs;

public sealed record ItemDebitoDto(string CodigoProduto, int Quantidade);
public sealed record DebitarSaldoRequest(string IdempotencyKey, List<ItemDebitoDto> Itens);
