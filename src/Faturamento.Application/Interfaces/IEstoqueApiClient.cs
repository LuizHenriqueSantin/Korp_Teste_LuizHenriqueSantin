using Faturamento.Application.DTOs;

namespace Faturamento.Application.Interfaces;

public interface IEstoqueApiClient
{
    Task<EstoqueDebitoResultado> DebitarSaldoAsync(
        string idempotencyKey,
        IEnumerable<ItemNotaFiscalDto> itens,
        CancellationToken ct = default);
}
