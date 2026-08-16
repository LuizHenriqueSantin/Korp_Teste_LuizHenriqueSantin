using Faturamento.Application.DTOs;
using MediatR;

namespace Faturamento.Application.Commands.CriarNotaFiscal;

public sealed record CriarNotaFiscalCommand(List<ItemRequestDto> Itens) : IRequest<int?>;
