using Faturamento.Application.DTOs;
using Faturamento.Application.Interfaces;
using Faturamento.Application.Notifications;
using Faturamento.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Faturamento.Application.Commands.ImprimirNotaFiscal;

public sealed class ImprimirNotaFiscalCommandHandler : IRequestHandler<ImprimirNotaFiscalCommand, bool>
{
    private readonly INotaFiscalRepository _notaFiscalRepository;
    private readonly IEstoqueApiClient _estoqueApiClient;
    private readonly IMediator _mediator;
    private readonly ILogger<ImprimirNotaFiscalCommandHandler> _logger;

    public ImprimirNotaFiscalCommandHandler(
        INotaFiscalRepository notaFiscalRepository,
        IEstoqueApiClient estoqueApiClient,
        IMediator mediator,
        ILogger<ImprimirNotaFiscalCommandHandler> logger)
    {
        _notaFiscalRepository = notaFiscalRepository;
        _estoqueApiClient = estoqueApiClient;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<bool> Handle(ImprimirNotaFiscalCommand request, CancellationToken cancellationToken)
    {
        var notaFiscal = await _notaFiscalRepository.ObterPorIdAsync(request.NotaFiscalId, cancellationToken);
        if (notaFiscal is null)
        {
            await _mediator.Publish(
                new DomainNotification("notaFiscal", $"Nota fiscal '{request.NotaFiscalId}' nao encontrada."),
                cancellationToken);
            return false;
        }

        if (notaFiscal.Status != StatusNotaFiscal.Aberta)
        {
            await _mediator.Publish(
                new DomainNotification("status", $"A nota fiscal numero '{notaFiscal.Numero}' nao esta Aberta."),
                cancellationToken);
            return false;
        }

        var itensDto = notaFiscal.Itens
            .Select(i => new ItemNotaFiscalDto(i.CodigoProduto, i.Quantidade))
            .ToList();

        var idempotencyKey = $"nota-fiscal-{notaFiscal.Id}";

        var resultado = await _estoqueApiClient.DebitarSaldoAsync(idempotencyKey, itensDto, cancellationToken);
        if (!resultado.Sucesso)
        {
            await _mediator.Publish(
                new DomainNotification("estoque", resultado.MensagemErro ?? "Falha ao debitar saldo no Estoque."),
                cancellationToken);
            return false;
        }

        notaFiscal.Fechar();

        try
        {
            await _notaFiscalRepository.SalvarAlteracoesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(
                ex,
                "Falha ao persistir o fechamento da nota fiscal {NotaFiscalId} apos debito confirmado " +
                "no Estoque (idempotencyKey={IdempotencyKey}). A nota permanece Aberta; uma nova tentativa " +
                "de impressao e segura e ira apenas concluir o fechamento.",
                notaFiscal.Id, idempotencyKey);

            await _mediator.Publish(
                new DomainNotification(
                    "notaFiscal",
                    "O saldo ja foi debitado, mas houve uma falha ao concluir o fechamento da nota. " +
                    "Tente imprimir novamente - a operacao e segura e nao ira debitar o saldo duas vezes."),
                cancellationToken);

            return false;
        }

        return true;
    }
}
