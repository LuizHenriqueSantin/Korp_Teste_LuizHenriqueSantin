namespace Faturamento.Domain.Exceptions;

public sealed class NotaFiscalJaFechadaException : Exception
{
    public NotaFiscalJaFechadaException(int numero)
        : base($"A nota fiscal numero '{numero}' ja esta Fechada e nao pode ser alterada.")
    {
    }
}
