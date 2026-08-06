using Notification.Domain;

namespace Notification.Application;

public sealed class EventPublisher
{
    public void Publish(Notification.Domain.Notification notification, string @event)
    {
        notification.AuditEvents.Add(@event);
    }

    public void Publish(NotificationTemplate template, string @event)
    {
        template.AuditEvents.Add(@event);
    }
}
