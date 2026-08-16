using Faturamento.Application.Commands.CriarNotaFiscal;
using Faturamento.Application.DTOs;
using Faturamento.Application.Interfaces;
using Faturamento.Domain.Entities;
using Moq;
using Xunit;

namespace Faturamento.Application.Tests.Commands;

public class CriarNotaFiscalCommandHandlerTests
{
    private readonly Mock<INotaFiscalRepository> _notaFiscalRepository = new();

    [Fact]
    public async Task Handle_DeveCriarNotaComItensERetornarId()
    {
        NotaFiscal? notaAdicionada = null;
        _notaFiscalRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<NotaFiscal>(), It.IsAny<CancellationToken>()))
            .Callback<NotaFiscal, CancellationToken>((nota, _) => notaAdicionada = nota)
            .Returns(Task.CompletedTask);

        var handler = new CriarNotaFiscalCommandHandler(_notaFiscalRepository.Object);
        var command = new CriarNotaFiscalCommand([
            new ItemRequestDto("P001", 2),
            new ItemRequestDto("P002", 1),
        ]);

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(notaAdicionada);
        Assert.Equal(2, notaAdicionada!.Itens.Count);
        _notaFiscalRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DevePropagarExcecao_QuandoNotaSemItens()
    {
        var handler = new CriarNotaFiscalCommandHandler(_notaFiscalRepository.Object);
        var command = new CriarNotaFiscalCommand([]);

        await Assert.ThrowsAsync<Faturamento.Domain.Exceptions.NotaFiscalSemItensException>(
            () => handler.Handle(command, CancellationToken.None));

        _notaFiscalRepository.Verify(r => r.AdicionarAsync(It.IsAny<NotaFiscal>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
