using MediatR;

namespace Estoque.Application.Notifications;

public sealed class DomainNotification : INotification
{
    public string Chave { get; }
    public string Mensagem { get; }
    public DateTime Timestamp { get; }

    public DomainNotification(string chave, string mensagem)
    {
        Chave = chave;
        Mensagem = mensagem;
        Timestamp = DateTime.UtcNow;
    }
}
