namespace Faturamento.Domain.Entities;

public class ItemNotaFiscal
{
    public int Id { get; private set; }
    public int NotaFiscalId { get; private set; }
    public string CodigoProduto { get; private set; } = string.Empty;
    public int Quantidade { get; private set; }

    private ItemNotaFiscal() { }

    public ItemNotaFiscal(string codigoProduto, int quantidade)
    {
        if (string.IsNullOrWhiteSpace(codigoProduto))
        {
            throw new ArgumentException("Codigo do produto e obrigatorio.", nameof(codigoProduto));
        }

        if (quantidade <= 0)
        {
            throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantidade));
        }

        CodigoProduto = codigoProduto;
        Quantidade = quantidade;
    }
}
