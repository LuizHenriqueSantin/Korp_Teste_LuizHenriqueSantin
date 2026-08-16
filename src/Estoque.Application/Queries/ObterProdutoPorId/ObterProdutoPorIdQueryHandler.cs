using Estoque.Application.DTOs;
using Estoque.Application.Interfaces;
using MediatR;

namespace Estoque.Application.Queries.ObterProdutoPorId;

public sealed class ObterProdutoPorIdQueryHandler : IRequestHandler<ObterProdutoPorIdQuery, ProdutoDto?>
{
    private readonly IProdutoRepository _repository;

    public ObterProdutoPorIdQueryHandler(IProdutoRepository repository) => _repository = repository;

    public async Task<ProdutoDto?> Handle(ObterProdutoPorIdQuery request, CancellationToken cancellationToken)
    {
        var produto = await _repository.ObterPorIdAsync(request.Id, cancellationToken);

        return produto is null
            ? null
            : new ProdutoDto(produto.Id, produto.Codigo, produto.Descricao, produto.Saldo);
    }
}
