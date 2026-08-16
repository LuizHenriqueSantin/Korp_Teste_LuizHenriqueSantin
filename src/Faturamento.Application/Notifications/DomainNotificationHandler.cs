using MediatR;

namespace Faturamento.Application.Notifications;

public interface IDomainNotificationHandler : INotificationHandler<DomainNotification>
{
    List<DomainNotification> GetNotifications();
    bool HasNotifications();
}

public sealed class DomainNotificationHandler : IDomainNotificationHandler
{
    private readonly List<DomainNotification> _notifications = [];

    public Task Handle(DomainNotification notification, CancellationToken cancellationToken)
    {
        _notifications.Add(notification);
        return Task.CompletedTask;
    }

    public List<DomainNotification> GetNotifications() => _notifications;

    public bool HasNotifications() => _notifications.Count > 0;
}
