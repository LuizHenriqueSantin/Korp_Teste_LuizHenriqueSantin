namespace Estoque.Domain.Exceptions;

public class ProdutoNaoEncontradoException : Exception
{
    public ProdutoNaoEncontradoException(string identificador)
        : base($"Produto '{identificador}' nao encontrado.")
    {
    }
}
