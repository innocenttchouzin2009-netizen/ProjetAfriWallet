using UniversalWallet.Api.Notifications.Application;
using UniversalWallet.Api.Notifications.Domain;

namespace UniversalWallet.Api.Notifications.Infrastructure;

public sealed class InMemoryNotificationRepository : INotificationRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, Notification> _notifications = new();
    private readonly Dictionary<string, Guid> _byKey = new(StringComparer.OrdinalIgnoreCase);

    public Task<Notification?> GetAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_notifications.TryGetValue(notificationId, out var notification) ? notification : null);
        }
    }

    public Task<IReadOnlyList<Notification>> ListAsync(Guid userAwid, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var items = _notifications.Values
                .Where(n => n.UserAwid == userAwid)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
            return Task.FromResult<IReadOnlyList<Notification>>(items);
        }
    }

    public Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _notifications[notification.NotificationId] = notification;
            _byKey[notification.NotificationKey] = notification.NotificationId;
            return Task.CompletedTask;
        }
    }

    public Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _notifications[notification.NotificationId] = notification;
            _byKey[notification.NotificationKey] = notification.NotificationId;
            return Task.CompletedTask;
        }
    }

    public Task<Notification?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_byKey.TryGetValue(key, out var notificationId) && _notifications.TryGetValue(notificationId, out var notification) ? notification : null);
        }
    }
}
