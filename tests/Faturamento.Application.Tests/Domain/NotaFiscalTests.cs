using Faturamento.Domain.Entities;
using Faturamento.Domain.Enums;
using Faturamento.Domain.Exceptions;
using Xunit;

namespace Faturamento.Application.Tests.Domain;

public class NotaFiscalTests
{
    [Fact]
    public void Construtor_DeveIniciarAbertaComItensEData()
    {
        var itens = new List<ItemNotaFiscal> { new("P001", 2) };

        var nota = new NotaFiscal(itens);

        Assert.Equal(StatusNotaFiscal.Aberta, nota.Status);
        Assert.Null(nota.DataFechamentoUtc);
        Assert.Single(nota.Itens);
        Assert.True(nota.DataCriacaoUtc <= DateTime.UtcNow);
    }

    [Fact]
    public void Construtor_DeveLancarExcecao_QuandoSemItens()
    {
        Assert.Throws<NotaFiscalSemItensException>(() => new NotaFiscal([]));
    }

    [Fact]
    public void Fechar_DeveAlterarStatusEDataFechamento_QuandoAberta()
    {
        var nota = new NotaFiscal([new ItemNotaFiscal("P001", 2)]);

        nota.Fechar();

        Assert.Equal(StatusNotaFiscal.Fechada, nota.Status);
        Assert.NotNull(nota.DataFechamentoUtc);
    }

    [Fact]
    public void Fechar_DeveLancarExcecao_QuandoJaFechada()
    {
        var nota = new NotaFiscal([new ItemNotaFiscal("P001", 2)]);
        nota.Fechar();

        Assert.Throws<NotaFiscalJaFechadaException>(() => nota.Fechar());
    }
}

public class ItemNotaFiscalTests
{
    [Fact]
    public void Construtor_DeveCriarItem_QuandoDadosValidos()
    {
        var item = new ItemNotaFiscal("P001", 3);

        Assert.Equal("P001", item.CodigoProduto);
        Assert.Equal(3, item.Quantidade);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Construtor_DeveLancarExcecao_QuandoCodigoInvalido(string? codigo)
    {
        Assert.Throws<ArgumentException>(() => new ItemNotaFiscal(codigo!, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Construtor_DeveLancarExcecao_QuandoQuantidadeInvalida(int quantidade)
    {
        Assert.Throws<ArgumentException>(() => new ItemNotaFiscal("P001", quantidade));
    }
}
