using Estoque.Application.DTOs;
using Estoque.Application.Interfaces;
using MediatR;

namespace Estoque.Application.Queries.ListarProdutos;

public sealed class ListarProdutosQueryHandler : IRequestHandler<ListarProdutosQuery, List<ProdutoDto>>
{
    private readonly IProdutoRepository _repository;

    public ListarProdutosQueryHandler(IProdutoRepository repository) => _repository = repository;

    public async Task<List<ProdutoDto>> Handle(ListarProdutosQuery request, CancellationToken cancellationToken)
    {
        var produtos = await _repository.ListarAsync(cancellationToken);

        return produtos
            .Select(p => new ProdutoDto(p.Id, p.Codigo, p.Descricao, p.Saldo))
            .OrderBy(p => p.Codigo)
            .ToList();
    }
}
