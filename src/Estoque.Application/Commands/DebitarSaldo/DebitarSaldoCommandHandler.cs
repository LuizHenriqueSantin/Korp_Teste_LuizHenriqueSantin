using Estoque.Application.Exceptions;
using Estoque.Application.Interfaces;
using Estoque.Application.Notifications;
using Estoque.Domain.Exceptions;
using MediatR;

namespace Estoque.Application.Commands.DebitarSaldo;

public sealed class DebitarSaldoCommandHandler : IRequestHandler<DebitarSaldoCommand, bool>
{
    private const int MaxTentativasConcorrencia = 3;

    private readonly IProdutoRepository _produtoRepository;
    private readonly IIdempotencyService _idempotencyService;
    private readonly IMediator _mediator;

    public DebitarSaldoCommandHandler(
        IProdutoRepository produtoRepository,
        IIdempotencyService idempotencyService,
        IMediator mediator)
    {
        _produtoRepository = produtoRepository;
        _idempotencyService = idempotencyService;
        _mediator = mediator;
    }

    public async Task<bool> Handle(DebitarSaldoCommand request, CancellationToken cancellationToken)
    {

        if (await _idempotencyService.JaProcessadoAsync(request.IdempotencyKey, cancellationToken))
        {
            return true;
        }

        var codigosSolicitados = request.Itens.Select(i => i.CodigoProduto).Distinct().ToList();
        var produtosEncontrados = await _produtoRepository.ObterPorCodigosAsync(codigosSolicitados, cancellationToken);
        var produtosPorCodigo = produtosEncontrados.ToDictionary(p => p.Codigo);

        var codigosNaoEncontrados = codigosSolicitados.Where(c => !produtosPorCodigo.ContainsKey(c)).ToList();
        if (codigosNaoEncontrados.Count > 0)
        {
            foreach (var codigo in codigosNaoEncontrados)
            {
                await _mediator.Publish(
                    new DomainNotification("produto", $"Produto '{codigo}' nao encontrado."), cancellationToken);
            }

            return false;
        }

        var quantidadesPorCodigo = request.Itens.ToDictionary(i => i.CodigoProduto, i => i.Quantidade);

        if (!TentarDebitarTodos(produtosPorCodigo, quantidadesPorCodigo, out var notificacaoSaldo))
        {
            await _mediator.Publish(notificacaoSaldo!, cancellationToken);
            return false;
        }

        _idempotencyService.MarcarComoProcessado(request.IdempotencyKey);

        for (var tentativa = 1; tentativa <= MaxTentativasConcorrencia; tentativa++)
        {
            try
            {
                await _produtoRepository.SalvarAlteracoesAsync(cancellationToken);
                return true;
            }
            catch (ConcurrencyConflictException ex) when (tentativa < MaxTentativasConcorrencia)
            {
                foreach (var codigo in ex.CodigosConflitantes)
                {
                    var produto = produtosPorCodigo[codigo];
                    var quantidade = quantidadesPorCodigo[codigo];

                    try
                    {
                        produto.DebitarSaldo(quantidade);
                    }
                    catch (SaldoInsuficienteException saldoEx)
                    {
                        await _mediator.Publish(new DomainNotification("saldo", saldoEx.Message), cancellationToken);
                        return false;
                    }
                }
            }
            catch (ConcurrencyConflictException)
            {
                await _mediator.Publish(
                    new DomainNotification("concorrencia",
                        $"Nao foi possivel concluir o debito apos {MaxTentativasConcorrencia} tentativas devido a concorrencia."),
                    cancellationToken);
                return false;
            }
        }

        return false;
    }

    private static bool TentarDebitarTodos(
        Dictionary<string, Domain.Entities.Produto> produtosPorCodigo,
        Dictionary<string, int> quantidadesPorCodigo,
        out DomainNotification? notificacao)
    {
        foreach (var (codigo, produto) in produtosPorCodigo)
        {
            try
            {
                produto.DebitarSaldo(quantidadesPorCodigo[codigo]);
            }
            catch (SaldoInsuficienteException ex)
            {
                notificacao = new DomainNotification("saldo", ex.Message);
                return false;
            }
        }

        notificacao = null;
        return true;
    }
}
