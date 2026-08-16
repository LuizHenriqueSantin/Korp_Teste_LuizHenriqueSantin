namespace Faturamento.Domain.Exceptions;

public sealed class NotaFiscalSemItensException : Exception
{
    public NotaFiscalSemItensException()
        : base("A nota fiscal deve conter ao menos um item.")
    {
    }
}
