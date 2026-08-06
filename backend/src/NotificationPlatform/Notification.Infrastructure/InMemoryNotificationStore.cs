using Notification.Domain;

namespace Notification.Infrastructure;

public sealed class InMemoryNotificationStore
{
    public List<Notification.Domain.Notification> Notifications { get; } = new();
    public List<NotificationTemplate> Templates { get; } = new();
    public List<NotificationPreference> Preferences { get; } = new();
}
