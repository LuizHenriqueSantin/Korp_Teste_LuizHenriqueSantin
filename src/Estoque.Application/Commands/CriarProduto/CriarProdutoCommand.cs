using MediatR;

namespace Estoque.Application.Commands.CriarProduto;

public sealed record CriarProdutoCommand(string Codigo, string Descricao, int SaldoInicial) : IRequest<int?>;
