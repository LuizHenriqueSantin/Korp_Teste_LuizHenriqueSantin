using Faturamento.Application.DTOs;
using MediatR;

namespace Faturamento.Application.Queries.ListarNotasFiscais;

public sealed record ListarNotasFiscaisQuery : IRequest<List<NotaFiscalDto>>;
