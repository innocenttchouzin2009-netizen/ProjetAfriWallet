using UniversalWallet.Api.Notifications.Application;
using UniversalWallet.Api.Notifications.Domain;

namespace UniversalWallet.Api.Notifications.Infrastructure;

public sealed class InMemoryNotificationPreferencesRepository : INotificationPreferencesRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, NotificationPreferences> _preferences = new();

    public Task<NotificationPreferences?> GetAsync(Guid userAwid, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_preferences.TryGetValue(userAwid, out var preferences) ? preferences : null);
        }
    }

    public Task SaveAsync(NotificationPreferences preferences, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _preferences[preferences.UserAwid] = preferences;
            return Task.CompletedTask;
        }
    }
}
