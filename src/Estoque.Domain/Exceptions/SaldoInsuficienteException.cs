namespace Estoque.Domain.Exceptions;

public class SaldoInsuficienteException : Exception
{
    public string CodigoProduto { get; }
    public int SaldoAtual { get; }
    public int QuantidadeSolicitada { get; }

    public SaldoInsuficienteException(string codigoProduto, int saldoAtual, int quantidadeSolicitada)
        : base($"Saldo insuficiente para o produto '{codigoProduto}'. Saldo atual: {saldoAtual}, solicitado: {quantidadeSolicitada}.")
    {
        CodigoProduto = codigoProduto;
        SaldoAtual = saldoAtual;
        QuantidadeSolicitada = quantidadeSolicitada;
    }
}
