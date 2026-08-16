using Estoque.Application.DTOs;
using MediatR;

namespace Estoque.Application.Commands.DebitarSaldo;

public sealed record DebitarSaldoCommand(string IdempotencyKey, List<ItemDebitoDto> Itens) : IRequest<bool>;
