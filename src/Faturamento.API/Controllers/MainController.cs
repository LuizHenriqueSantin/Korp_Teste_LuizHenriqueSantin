using Faturamento.Application.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace Faturamento.API.Controllers;

[ApiController]
public abstract class MainController : ControllerBase
{
    private readonly IDomainNotificationHandler _notifications;

    protected MainController(IDomainNotificationHandler notifications) => _notifications = notifications;

    protected bool OperacaoValida() => !_notifications.HasNotifications();

    protected IActionResult CustomResponse(object? result = null)
    {
        if (OperacaoValida())
        {
            return result is null ? NoContent() : Ok(result);
        }

        return BadRequest(new
        {
            errors = _notifications.GetNotifications().Select(n => new { n.Chave, n.Mensagem })
        });
    }

    protected IActionResult CustomResponse(int createdId, object result)
    {
        if (OperacaoValida())
        {
            return CreatedAtAction(null, new { id = createdId }, result);
        }

        return BadRequest(new
        {
            errors = _notifications.GetNotifications().Select(n => new { n.Chave, n.Mensagem })
        });
    }
}
