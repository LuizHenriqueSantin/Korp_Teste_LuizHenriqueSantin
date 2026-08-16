using Faturamento.Application.DTOs;
using Faturamento.Application.Interfaces;
using MediatR;

namespace Faturamento.Application.Queries.ListarNotasFiscais;

public sealed class ListarNotasFiscaisQueryHandler : IRequestHandler<ListarNotasFiscaisQuery, List<NotaFiscalDto>>
{
    private readonly INotaFiscalRepository _notaFiscalRepository;

    public ListarNotasFiscaisQueryHandler(INotaFiscalRepository notaFiscalRepository)
    {
        _notaFiscalRepository = notaFiscalRepository;
    }

    public async Task<List<NotaFiscalDto>> Handle(ListarNotasFiscaisQuery request, CancellationToken cancellationToken)
    {
        var notas = await _notaFiscalRepository.ListarAsync(cancellationToken);

        return notas
            .OrderByDescending(n => n.Numero)
            .Select(n => new NotaFiscalDto(
                n.Id,
                n.Numero,
                n.Status.ToString(),
                n.DataCriacaoUtc,
                n.DataFechamentoUtc,
                n.Itens.Select(i => new ItemNotaFiscalDto(i.CodigoProduto, i.Quantidade)).ToList()))
            .ToList();
    }
}
