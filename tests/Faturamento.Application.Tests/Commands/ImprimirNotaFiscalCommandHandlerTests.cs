using Faturamento.Application.Commands.ImprimirNotaFiscal;
using Faturamento.Application.DTOs;
using Faturamento.Application.Interfaces;
using Faturamento.Application.Notifications;
using Faturamento.Domain.Entities;
using Faturamento.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Faturamento.Application.Tests.Commands;

public class ImprimirNotaFiscalCommandHandlerTests
{
    private readonly Mock<INotaFiscalRepository> _notaFiscalRepository = new();
    private readonly Mock<IEstoqueApiClient> _estoqueApiClient = new();
    private readonly Mock<IMediator> _mediator = new();

    private ImprimirNotaFiscalCommandHandler CriarHandler() => new(
        _notaFiscalRepository.Object,
        _estoqueApiClient.Object,
        _mediator.Object,
        NullLogger<ImprimirNotaFiscalCommandHandler>.Instance);

    private static NotaFiscal CriarNotaAberta() => new([new ItemNotaFiscal("P001", 2)]);

    [Fact]
    public async Task Handle_DeveRetornarFalseEPublicarNotificacao_QuandoNotaNaoEncontrada()
    {
        _notaFiscalRepository.Setup(r => r.ObterPorIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotaFiscal?)null);

        var resultado = await CriarHandler().Handle(new ImprimirNotaFiscalCommand(99), CancellationToken.None);

        Assert.False(resultado);
        _mediator.Verify(m => m.Publish(
            It.Is<DomainNotification>(n => n.Chave == "notaFiscal"),
            It.IsAny<CancellationToken>()), Times.Once);
        _estoqueApiClient.Verify(c => c.DebitarSaldoAsync(
            It.IsAny<string>(), It.IsAny<IEnumerable<ItemNotaFiscalDto>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarFalseEPublicarNotificacao_QuandoNotaNaoEstaAberta()
    {
        var nota = CriarNotaAberta();
        nota.Fechar();

        _notaFiscalRepository.Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nota);

        var resultado = await CriarHandler().Handle(new ImprimirNotaFiscalCommand(1), CancellationToken.None);

        Assert.False(resultado);
        _mediator.Verify(m => m.Publish(
            It.Is<DomainNotification>(n => n.Chave == "status"),
            It.IsAny<CancellationToken>()), Times.Once);
        _estoqueApiClient.Verify(c => c.DebitarSaldoAsync(
            It.IsAny<string>(), It.IsAny<IEnumerable<ItemNotaFiscalDto>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarFalseEPublicarNotificacao_QuandoEstoqueFalhaAoDebitar()
    {
        var nota = CriarNotaAberta();

        _notaFiscalRepository.Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nota);
        _estoqueApiClient
            .Setup(c => c.DebitarSaldoAsync(It.IsAny<string>(), It.IsAny<IEnumerable<ItemNotaFiscalDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstoqueDebitoResultado(false, "Saldo insuficiente para o produto 'P001'."));

        var resultado = await CriarHandler().Handle(new ImprimirNotaFiscalCommand(1), CancellationToken.None);

        Assert.False(resultado);
        Assert.Equal(StatusNotaFiscal.Aberta, nota.Status); // nota nao pode ser fechada se o debito falhou
        _mediator.Verify(m => m.Publish(
            It.Is<DomainNotification>(n => n.Chave == "estoque"),
            It.IsAny<CancellationToken>()), Times.Once);
        _notaFiscalRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveFecharNotaERetornarTrue_QuandoDebitoComSucesso()
    {
        var nota = CriarNotaAberta();

        _notaFiscalRepository.Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nota);
        _estoqueApiClient
            .Setup(c => c.DebitarSaldoAsync(
                $"nota-fiscal-{nota.Id}",
                It.IsAny<IEnumerable<ItemNotaFiscalDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstoqueDebitoResultado(true, null));

        var resultado = await CriarHandler().Handle(new ImprimirNotaFiscalCommand(1), CancellationToken.None);

        Assert.True(resultado);
        Assert.Equal(StatusNotaFiscal.Fechada, nota.Status);
        Assert.NotNull(nota.DataFechamentoUtc);
        _notaFiscalRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mediator.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarFalseEPublicarNotificacao_QuandoSalvarFalhaAposDebitoConfirmado()
    {
        var nota = CriarNotaAberta();

        _notaFiscalRepository.Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nota);
        _estoqueApiClient
            .Setup(c => c.DebitarSaldoAsync(It.IsAny<string>(), It.IsAny<IEnumerable<ItemNotaFiscalDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstoqueDebitoResultado(true, null));
        _notaFiscalRepository
            .Setup(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Falha simulada de banco de dados."));

        var resultado = await CriarHandler().Handle(new ImprimirNotaFiscalCommand(1), CancellationToken.None);

        Assert.False(resultado);
        _mediator.Verify(m => m.Publish(
            It.Is<DomainNotification>(n => n.Chave == "notaFiscal" &&
                n.Mensagem.Contains("saldo ja foi debitado", StringComparison.OrdinalIgnoreCase)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
