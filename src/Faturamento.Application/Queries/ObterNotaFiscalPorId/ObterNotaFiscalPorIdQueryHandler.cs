using Faturamento.Application.DTOs;
using Faturamento.Application.Interfaces;
using MediatR;

namespace Faturamento.Application.Queries.ObterNotaFiscalPorId;

public sealed class ObterNotaFiscalPorIdQueryHandler
    : IRequestHandler<ObterNotaFiscalPorIdQuery, NotaFiscalDto?>
{
    private readonly INotaFiscalRepository _notaFiscalRepository;

    public ObterNotaFiscalPorIdQueryHandler(INotaFiscalRepository notaFiscalRepository)
    {
        _notaFiscalRepository = notaFiscalRepository;
    }

    public async Task<NotaFiscalDto?> Handle(ObterNotaFiscalPorIdQuery request, CancellationToken cancellationToken)
    {
        var nota = await _notaFiscalRepository.ObterPorIdAsync(request.Id, cancellationToken);
        if (nota is null)
        {
            return null;
        }

        return new NotaFiscalDto(
            nota.Id,
            nota.Numero,
            nota.Status.ToString(),
            nota.DataCriacaoUtc,
            nota.DataFechamentoUtc,
            nota.Itens.Select(i => new ItemNotaFiscalDto(i.CodigoProduto, i.Quantidade)).ToList());
    }
}
