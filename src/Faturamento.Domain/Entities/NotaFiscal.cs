using Faturamento.Domain.Enums;
using Faturamento.Domain.Exceptions;

namespace Faturamento.Domain.Entities;

public class NotaFiscal
{
    private readonly List<ItemNotaFiscal> _itens = [];

    public int Id { get; private set; }
    public int Numero { get; private set; }

    public StatusNotaFiscal Status { get; private set; }
    public DateTime DataCriacaoUtc { get; private set; }
    public DateTime? DataFechamentoUtc { get; private set; }

    public IReadOnlyCollection<ItemNotaFiscal> Itens => _itens.AsReadOnly();

    private NotaFiscal() { }

    public NotaFiscal(IEnumerable<ItemNotaFiscal> itens)
    {
        var itensList = itens?.ToList() ?? [];
        if (itensList.Count == 0)
        {
            throw new NotaFiscalSemItensException();
        }

        _itens.AddRange(itensList);
        Status = StatusNotaFiscal.Aberta;
        DataCriacaoUtc = DateTime.UtcNow;
    }

    public void Fechar()
    {
        if (Status != StatusNotaFiscal.Aberta)
        {
            throw new NotaFiscalJaFechadaException(Numero);
        }

        Status = StatusNotaFiscal.Fechada;
        DataFechamentoUtc = DateTime.UtcNow;
    }
}
