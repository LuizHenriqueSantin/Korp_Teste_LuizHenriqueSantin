using Estoque.Domain.Entities;
using Estoque.Domain.Exceptions;
using Xunit;

namespace Estoque.Application.Tests.Domain;

public class ProdutoTests
{
    [Fact]
    public void Construtor_DeveCriarProdutoComSaldoInicial()
    {
        var produto = new Produto("P001", "Produto Teste", 10);

        Assert.Equal("P001", produto.Codigo);
        Assert.Equal("Produto Teste", produto.Descricao);
        Assert.Equal(10, produto.Saldo);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Construtor_DeveLancarExcecao_QuandoCodigoInvalido(string? codigo)
    {
        Assert.Throws<ArgumentException>(() => new Produto(codigo!, "Descricao", 10));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Construtor_DeveLancarExcecao_QuandoDescricaoInvalida(string? descricao)
    {
        Assert.Throws<ArgumentException>(() => new Produto("P001", descricao!, 10));
    }

    [Fact]
    public void Construtor_DeveLancarExcecao_QuandoSaldoInicialNegativo()
    {
        Assert.Throws<ArgumentException>(() => new Produto("P001", "Descricao", -1));
    }

    [Fact]
    public void DebitarSaldo_DeveReduzirSaldo_QuandoQuantidadeMenorOuIgualAoSaldo()
    {
        var produto = new Produto("P001", "Produto Teste", 10);

        produto.DebitarSaldo(2);

        Assert.Equal(8, produto.Saldo);
    }

    [Fact]
    public void DebitarSaldo_DevePermitirZerarSaldo()
    {
        var produto = new Produto("P001", "Produto Teste", 5);

        produto.DebitarSaldo(5);

        Assert.Equal(0, produto.Saldo);
    }

    [Fact]
    public void DebitarSaldo_DeveLancarSaldoInsuficiente_QuandoQuantidadeMaiorQueSaldo()
    {
        var produto = new Produto("P001", "Produto Teste", 1);

        var ex = Assert.Throws<SaldoInsuficienteException>(() => produto.DebitarSaldo(2));

        Assert.Equal("P001", ex.CodigoProduto);
        Assert.Equal(1, ex.SaldoAtual);
        Assert.Equal(2, ex.QuantidadeSolicitada);
        Assert.Equal(1, produto.Saldo); // saldo nao deve ser alterado em caso de falha
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DebitarSaldo_DeveLancarExcecao_QuandoQuantidadeInvalida(int quantidade)
    {
        var produto = new Produto("P001", "Produto Teste", 10);

        Assert.Throws<ArgumentException>(() => produto.DebitarSaldo(quantidade));
    }

    [Fact]
    public void ReporSaldo_DeveAumentarSaldo()
    {
        var produto = new Produto("P001", "Produto Teste", 10);

        produto.ReporSaldo(5);

        Assert.Equal(15, produto.Saldo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReporSaldo_DeveLancarExcecao_QuandoQuantidadeInvalida(int quantidade)
    {
        var produto = new Produto("P001", "Produto Teste", 10);

        Assert.Throws<ArgumentException>(() => produto.ReporSaldo(quantidade));
    }
}
