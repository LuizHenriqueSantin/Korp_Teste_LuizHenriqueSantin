using Faturamento.Application.Commands.CriarNotaFiscal;
using Faturamento.Application.DTOs;
using Xunit;

namespace Faturamento.Application.Tests.Commands;

public class CriarNotaFiscalCommandValidatorTests
{
    private readonly CriarNotaFiscalCommandValidator _validator = new();

    [Fact]
    public void Validate_DeveFalhar_QuandoNotaSemItens()
    {
        var resultado = _validator.Validate(new CriarNotaFiscalCommand([]));

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Itens");
    }

    [Fact]
    public void Validate_DeveFalhar_QuandoCodigoProdutoVazio()
    {
        var resultado = _validator.Validate(new CriarNotaFiscalCommand([new ItemRequestDto("", 1)]));

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Itens[0].CodigoProduto");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_DeveFalhar_QuandoQuantidadeInvalida(int quantidade)
    {
        var resultado = _validator.Validate(new CriarNotaFiscalCommand([new ItemRequestDto("P001", quantidade)]));

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Itens[0].Quantidade");
    }

    [Fact]
    public void Validate_DevePassar_QuandoComandoValido()
    {
        var resultado = _validator.Validate(new CriarNotaFiscalCommand([new ItemRequestDto("P001", 3)]));

        Assert.True(resultado.IsValid);
    }
}
