using Faturamento.Domain.Entities;

namespace Faturamento.Application.Interfaces;

public interface INotaFiscalRepository
{
    Task<NotaFiscal?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<List<NotaFiscal>> ListarAsync(CancellationToken ct = default);
    Task AdicionarAsync(NotaFiscal notaFiscal, CancellationToken ct = default);
    Task SalvarAlteracoesAsync(CancellationToken ct = default);
}
