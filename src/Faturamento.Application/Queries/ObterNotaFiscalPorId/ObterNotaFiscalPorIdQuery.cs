using Faturamento.Application.DTOs;
using MediatR;

namespace Faturamento.Application.Queries.ObterNotaFiscalPorId;

public sealed record ObterNotaFiscalPorIdQuery(int Id) : IRequest<NotaFiscalDto?>;
