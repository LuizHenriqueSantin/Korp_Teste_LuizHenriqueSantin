using Faturamento.Application.Interfaces;
using Faturamento.Domain.Entities;
using Faturamento.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Faturamento.Infrastructure.Repositories;

public class NotaFiscalRepository : INotaFiscalRepository
{
    private readonly FaturamentoDbContext _context;

    public NotaFiscalRepository(FaturamentoDbContext context) => _context = context;

    public Task<NotaFiscal?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        _context.NotasFiscais
            .Include(n => n.Itens)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<List<NotaFiscal>> ListarAsync(CancellationToken ct = default) =>
        await _context.NotasFiscais
            .Include(n => n.Itens)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task AdicionarAsync(NotaFiscal notaFiscal, CancellationToken ct = default) =>
        await _context.NotasFiscais.AddAsync(notaFiscal, ct);

    public Task SalvarAlteracoesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}
