using Estoque.Application.Commands.DebitarSaldo;
using Estoque.Application.DTOs;
using Estoque.Application.Exceptions;
using Estoque.Application.Interfaces;
using Estoque.Application.Notifications;
using Estoque.Domain.Entities;
using MediatR;
using Moq;
using Xunit;

namespace Estoque.Application.Tests.Commands;

public class DebitarSaldoCommandHandlerTests
{
    private readonly Mock<IProdutoRepository> _produtoRepository = new();
    private readonly Mock<IIdempotencyService> _idempotencyService = new();
    private readonly Mock<IMediator> _mediator = new();

    private DebitarSaldoCommandHandler CriarHandler() =>
        new(_produtoRepository.Object, _idempotencyService.Object, _mediator.Object);

    private static Produto CriarProduto(string codigo, int saldo) => new(codigo, $"Produto {codigo}", saldo);

    [Fact]
    public async Task Handle_DeveRetornarTrueSemDebitar_QuandoIdempotencyKeyJaProcessada()
    {
        _idempotencyService.Setup(s => s.JaProcessadoAsync("chave-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new DebitarSaldoCommand("chave-1", [new ItemDebitoDto("P001", 2)]);

        var resultado = await CriarHandler().Handle(command, CancellationToken.None);

        Assert.True(resultado);
        _produtoRepository.Verify(r => r.ObterPorCodigosAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        _produtoRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DevePublicarNotificacaoERetornarFalse_QuandoProdutoNaoEncontrado()
    {
        _idempotencyService.Setup(s => s.JaProcessadoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _produtoRepository.Setup(r => r.ObterPorCodigosAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var command = new DebitarSaldoCommand("chave-1", [new ItemDebitoDto("P404", 1)]);

        var resultado = await CriarHandler().Handle(command, CancellationToken.None);

        Assert.False(resultado);
        _mediator.Verify(m => m.Publish(
            It.Is<DomainNotification>(n => n.Chave == "produto"),
            It.IsAny<CancellationToken>()), Times.Once);
        _produtoRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DevePublicarNotificacaoERetornarFalse_QuandoSaldoInsuficiente()
    {
        var produto = CriarProduto("P001", 1);

        _idempotencyService.Setup(s => s.JaProcessadoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _produtoRepository.Setup(r => r.ObterPorCodigosAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([produto]);

        var command = new DebitarSaldoCommand("chave-1", [new ItemDebitoDto("P001", 2)]);

        var resultado = await CriarHandler().Handle(command, CancellationToken.None);

        Assert.False(resultado);
        Assert.Equal(1, produto.Saldo); // saldo permanece intacto
        _mediator.Verify(m => m.Publish(
            It.Is<DomainNotification>(n => n.Chave == "saldo"),
            It.IsAny<CancellationToken>()), Times.Once);
        _idempotencyService.Verify(s => s.MarcarComoProcessado(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveDebitarEMarcarIdempotente_QuandoSaldoSuficiente()
    {
        var produto = CriarProduto("P001", 10);

        _idempotencyService.Setup(s => s.JaProcessadoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _produtoRepository.Setup(r => r.ObterPorCodigosAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([produto]);
        _produtoRepository.Setup(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new DebitarSaldoCommand("nota-fiscal-1", [new ItemDebitoDto("P001", 3)]);

        var resultado = await CriarHandler().Handle(command, CancellationToken.None);

        Assert.True(resultado);
        Assert.Equal(7, produto.Saldo);
        _idempotencyService.Verify(s => s.MarcarComoProcessado("nota-fiscal-1"), Times.Once);
        _mediator.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveTentarNovamenteERepetirDebito_QuandoConflitoDeConcorrenciaNaPrimeiraTentativa()
    {
        var produto = CriarProduto("P001", 10);

        _idempotencyService.Setup(s => s.JaProcessadoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _produtoRepository.Setup(r => r.ObterPorCodigosAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([produto]);

        var chamadas = 0;
        _produtoRepository.Setup(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                chamadas++;
                if (chamadas == 1)
                {
                    throw new ConcurrencyConflictException("Conflito", ["P001"]);
                }

                return true;
            });

        var command = new DebitarSaldoCommand("nota-fiscal-1", [new ItemDebitoDto("P001", 3)]);

        var resultado = await CriarHandler().Handle(command, CancellationToken.None);

        Assert.True(resultado);
        Assert.Equal(2, chamadas);
        // o debito foi reaplicado apos o reload simulado do conflito: 10 -> 7 (1a tentativa) -> 4 (retry)
        Assert.Equal(4, produto.Saldo);
    }

    [Fact]
    public async Task Handle_DevePublicarNotificacaoERetornarFalse_QuandoConcorrenciaEsgotaTentativas()
    {
        var produto = CriarProduto("P001", 10);

        _idempotencyService.Setup(s => s.JaProcessadoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _produtoRepository.Setup(r => r.ObterPorCodigosAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([produto]);
        _produtoRepository.Setup(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("Conflito", ["P001"]));

        var command = new DebitarSaldoCommand("nota-fiscal-1", [new ItemDebitoDto("P001", 3)]);

        var resultado = await CriarHandler().Handle(command, CancellationToken.None);

        Assert.False(resultado);
        _produtoRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
        _mediator.Verify(m => m.Publish(
            It.Is<DomainNotification>(n => n.Chave == "concorrencia"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
