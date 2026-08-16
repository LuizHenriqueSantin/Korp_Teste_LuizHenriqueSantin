using Estoque.Application.Commands.CriarProduto;
using Estoque.Application.Commands.DebitarSaldo;
using Estoque.Application.DTOs;
using Estoque.Application.Notifications;
using Estoque.Application.Queries.ListarProdutos;
using Estoque.Application.Queries.ObterProdutoPorId;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Estoque.API.Controllers;

[Route("produtos")]
public class ProdutosController : MainController
{
    private readonly IMediator _mediator;

    public ProdutosController(IMediator mediator, IDomainNotificationHandler notifications)
        : base(notifications)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ProdutoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var produtos = await _mediator.Send(new ListarProdutosQuery(), ct);
        return CustomResponse(produtos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProdutoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(int id, CancellationToken ct)
    {
        var produto = await _mediator.Send(new ObterProdutoPorIdQuery(id), ct);
        return produto is null ? NotFound() : CustomResponse(produto);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar([FromBody] CriarProdutoCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CustomResponse(id ?? 0, new { id });
    }

    [HttpPost("debitar-saldo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DebitarSaldo([FromBody] DebitarSaldoCommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);
        return CustomResponse();
    }
}
