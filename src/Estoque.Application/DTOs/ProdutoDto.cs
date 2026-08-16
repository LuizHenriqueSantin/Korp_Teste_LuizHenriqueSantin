namespace Estoque.Application.DTOs;

public sealed record ProdutoDto(int Id, string Codigo, string Descricao, int Saldo);
