using Estoque.Application.Interfaces;
using Estoque.Application.Notifications;
using Estoque.Domain.Entities;
using MediatR;

namespace Estoque.Application.Commands.CriarProduto;

public sealed class CriarProdutoCommandHandler : IRequestHandler<CriarProdutoCommand, int?>
{
    private readonly IProdutoRepository _repository;
    private readonly IMediator _mediator;

    public CriarProdutoCommandHandler(IProdutoRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task<int?> Handle(CriarProdutoCommand request, CancellationToken cancellationToken)
    {
        var existente = await _repository.ObterPorCodigoAsync(request.Codigo, cancellationToken);
        if (existente is not null)
        {
            await _mediator.Publish(
                new DomainNotification("codigo", $"Ja existe um produto cadastrado com o codigo '{request.Codigo}'."),
                cancellationToken);
            return null;
        }

        var produto = new Produto(request.Codigo, request.Descricao, request.SaldoInicial);

        await _repository.AdicionarAsync(produto, cancellationToken);
        await _repository.SalvarAlteracoesAsync(cancellationToken);

        return produto.Id;
    }
}
