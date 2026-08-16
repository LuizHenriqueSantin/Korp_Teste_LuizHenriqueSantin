using Estoque.Application.Interfaces;
using Estoque.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Estoque.Infrastructure.Repositories;

public class IdempotencyService : IIdempotencyService
{
    private readonly EstoqueDbContext _context;

    public IdempotencyService(EstoqueDbContext context) => _context = context;

    public Task<bool> JaProcessadoAsync(string idempotencyKey, CancellationToken ct = default) =>
        _context.IdempotencyRecords.AnyAsync(x => x.Key == idempotencyKey, ct);

    public void MarcarComoProcessado(string idempotencyKey)
    {
        _context.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Key = idempotencyKey,
            ProcessedAtUtc = DateTime.UtcNow
        });
    }
}
