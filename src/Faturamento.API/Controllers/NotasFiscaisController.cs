using Faturamento.Application.Commands.CriarNotaFiscal;
using Faturamento.Application.Commands.ImprimirNotaFiscal;
using Faturamento.Application.DTOs;
using Faturamento.Application.Notifications;
using Faturamento.Application.Queries.ListarNotasFiscais;
using Faturamento.Application.Queries.ObterNotaFiscalPorId;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Faturamento.API.Controllers;

[Route("notas-fiscais")]
public class NotasFiscaisController : MainController
{
    private readonly IMediator _mediator;

    public NotasFiscaisController(IMediator mediator, IDomainNotificationHandler notifications)
        : base(notifications)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<NotaFiscalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var notas = await _mediator.Send(new ListarNotasFiscaisQuery(), ct);
        return CustomResponse(notas);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(NotaFiscalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(int id, CancellationToken ct)
    {
        var nota = await _mediator.Send(new ObterNotaFiscalPorIdQuery(id), ct);
        return nota is null ? NotFound() : CustomResponse(nota);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar([FromBody] CriarNotaFiscalCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CustomResponse(id ?? 0, new { id });
    }

    [HttpPost("{id:int}/imprimir")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Imprimir(int id, CancellationToken ct)
    {
        await _mediator.Send(new ImprimirNotaFiscalCommand(id), ct);
        return CustomResponse();
    }
}
