using Estoque.Application.DTOs;
using MediatR;

namespace Estoque.Application.Queries.ListarProdutos;

public sealed record ListarProdutosQuery : IRequest<List<ProdutoDto>>;
