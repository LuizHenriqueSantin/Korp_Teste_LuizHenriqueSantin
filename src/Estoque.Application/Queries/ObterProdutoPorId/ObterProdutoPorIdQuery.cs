using Estoque.Application.DTOs;
using MediatR;

namespace Estoque.Application.Queries.ObterProdutoPorId;

public sealed record ObterProdutoPorIdQuery(int Id) : IRequest<ProdutoDto?>;
