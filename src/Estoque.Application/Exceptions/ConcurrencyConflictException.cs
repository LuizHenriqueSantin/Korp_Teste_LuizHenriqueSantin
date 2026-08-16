namespace Estoque.Application.Exceptions;

public sealed class ConcurrencyConflictException : Exception
{
    public IReadOnlyList<string> CodigosConflitantes { get; }

    public ConcurrencyConflictException(string message, IReadOnlyList<string> codigosConflitantes)
        : base(message)
    {
        CodigosConflitantes = codigosConflitantes;
    }
}
