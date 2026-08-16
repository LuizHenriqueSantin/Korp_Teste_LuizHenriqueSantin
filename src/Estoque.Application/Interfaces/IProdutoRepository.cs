using Estoque.Domain.Entities;

namespace Estoque.Application.Interfaces;

public interface IProdutoRepository
{
    Task<Produto?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<Produto?> ObterPorCodigoAsync(string codigo, CancellationToken ct = default);
    Task<List<Produto>> ObterPorCodigosAsync(IEnumerable<string> codigos, CancellationToken ct = default);
    Task<List<Produto>> ListarAsync(CancellationToken ct = default);
    Task AdicionarAsync(Produto produto, CancellationToken ct = default);
    Task<bool> SalvarAlteracoesAsync(CancellationToken ct = default);
}
