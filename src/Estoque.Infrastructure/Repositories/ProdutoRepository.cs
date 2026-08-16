using Estoque.Application.Exceptions;
using Estoque.Application.Interfaces;
using Estoque.Domain.Entities;
using Estoque.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Estoque.Infrastructure.Repositories;

public class ProdutoRepository : IProdutoRepository
{
    private readonly EstoqueDbContext _context;

    public ProdutoRepository(EstoqueDbContext context) => _context = context;

    public Task<Produto?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        _context.Produtos.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Produto?> ObterPorCodigoAsync(string codigo, CancellationToken ct = default) =>
        _context.Produtos.FirstOrDefaultAsync(p => p.Codigo == codigo, ct);

    public Task<List<Produto>> ObterPorCodigosAsync(IEnumerable<string> codigos, CancellationToken ct = default)
    {
        var lista = codigos as List<string> ?? codigos.ToList();
        return _context.Produtos.Where(p => lista.Contains(p.Codigo)).ToListAsync(ct);
    }

    public async Task<List<Produto>> ListarAsync(CancellationToken ct = default) =>
        await _context.Produtos.AsNoTracking().ToListAsync(ct);

    public async Task AdicionarAsync(Produto produto, CancellationToken ct = default) =>
        await _context.Produtos.AddAsync(produto, ct);

    public async Task<bool> SalvarAlteracoesAsync(CancellationToken ct = default)
    {
        try
        {
            await _context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var codigosConflitantes = new List<string>();

            foreach (var entry in ex.Entries)
            {
                await entry.ReloadAsync(ct);

                if (entry.Entity is Produto produto)
                {
                    codigosConflitantes.Add(produto.Codigo);
                }
            }

            throw new ConcurrencyConflictException(
                "O registro foi alterado por outra operacao antes que esta pudesse ser concluida.",
                codigosConflitantes);
        }
    }
}
