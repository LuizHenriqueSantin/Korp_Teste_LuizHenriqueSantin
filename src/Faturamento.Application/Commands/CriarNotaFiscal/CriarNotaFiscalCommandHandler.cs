using Faturamento.Application.Interfaces;
using Faturamento.Domain.Entities;
using MediatR;

namespace Faturamento.Application.Commands.CriarNotaFiscal;

public sealed class CriarNotaFiscalCommandHandler : IRequestHandler<CriarNotaFiscalCommand, int?>
{
    private readonly INotaFiscalRepository _notaFiscalRepository;

    public CriarNotaFiscalCommandHandler(INotaFiscalRepository notaFiscalRepository)
    {
        _notaFiscalRepository = notaFiscalRepository;
    }

    public async Task<int?> Handle(CriarNotaFiscalCommand request, CancellationToken cancellationToken)
    {
        var itens = request.Itens
            .Select(i => new ItemNotaFiscal(i.CodigoProduto, i.Quantidade))
            .ToList();

        var notaFiscal = new NotaFiscal(itens);

        await _notaFiscalRepository.AdicionarAsync(notaFiscal, cancellationToken);
        await _notaFiscalRepository.SalvarAlteracoesAsync(cancellationToken);

        return notaFiscal.Id;
    }
}
