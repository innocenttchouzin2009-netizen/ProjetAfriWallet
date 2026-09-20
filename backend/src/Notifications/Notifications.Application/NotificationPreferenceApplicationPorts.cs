using AfriWallet.Notifications.Domain;

namespace AfriWallet.Notifications.Application;

public interface INotificationPreferenceRepository
{
    Task<NotificationPreference?> GetAsync(
        Guid userId,
        NotificationChannel channel,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationPreference>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default);
    Task UpdateAsync(NotificationPreference preference, CancellationToken cancellationToken = default);
}

public interface INotificationChannelPolicyProvider
{
    NotificationChannelPolicy Get(NotificationChannel channel);
    IReadOnlyList<NotificationChannelPolicy> List();
}
