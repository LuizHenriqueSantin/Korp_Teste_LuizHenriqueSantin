using Estoque.Domain.Exceptions;

namespace Estoque.Domain.Entities;

public class Produto
{
    public int Id { get; private set; }
    public string Codigo { get; private set; } = string.Empty;
    public string Descricao { get; private set; } = string.Empty;
    public int Saldo { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private Produto() { }

    public Produto(string codigo, string descricao, int saldoInicial)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            throw new ArgumentException("Codigo do produto e obrigatorio.", nameof(codigo));

        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descricao do produto e obrigatoria.", nameof(descricao));

        if (saldoInicial < 0)
            throw new ArgumentException("Saldo inicial nao pode ser negativo.", nameof(saldoInicial));

        Codigo = codigo;
        Descricao = descricao;
        Saldo = saldoInicial;
    }

    public void DebitarSaldo(int quantidade)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade a debitar deve ser maior que zero.", nameof(quantidade));

        if (Saldo < quantidade)
            throw new SaldoInsuficienteException(Codigo, Saldo, quantidade);

        Saldo -= quantidade;
    }

    public void ReporSaldo(int quantidade)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade a repor deve ser maior que zero.", nameof(quantidade));

        Saldo += quantidade;
    }
}
