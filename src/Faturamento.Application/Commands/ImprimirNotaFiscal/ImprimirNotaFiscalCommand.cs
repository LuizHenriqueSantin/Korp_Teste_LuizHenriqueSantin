using MediatR;

namespace Faturamento.Application.Commands.ImprimirNotaFiscal;

public sealed record ImprimirNotaFiscalCommand(int NotaFiscalId) : IRequest<bool>;
