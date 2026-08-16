namespace Estoque.Application.Interfaces;

public interface IIdempotencyService
{
    Task<bool> JaProcessadoAsync(string idempotencyKey, CancellationToken ct = default);
    void MarcarComoProcessado(string idempotencyKey);
}
