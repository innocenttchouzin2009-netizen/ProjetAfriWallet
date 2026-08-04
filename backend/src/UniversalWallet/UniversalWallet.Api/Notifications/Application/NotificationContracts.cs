using UniversalWallet.Api.Notifications.Domain;

namespace UniversalWallet.Api.Notifications.Application;

public interface INotificationRepository
{
    Task<Notification?> GetAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Notification>> ListAsync(Guid userAwid, CancellationToken cancellationToken = default);
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<Notification?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
}

public interface INotificationPreferencesRepository
{
    Task<NotificationPreferences?> GetAsync(Guid userAwid, CancellationToken cancellationToken = default);
    Task SaveAsync(NotificationPreferences preferences, CancellationToken cancellationToken = default);
}

public interface INotificationChannelProvider
{
    string ChannelName { get; }
    Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default);
}

public sealed record CreateNotificationRequest(string EventType, Guid UserAwid, string Category, NotificationPriority Priority, string Title, string Body, string Payload, string CorrelationId, string? NotificationKey = null);
public sealed record CreateNotificationResponse(Notification Notification);
public sealed record ReadNotificationRequest(Guid NotificationId);
public sealed record ReadAllNotificationsRequest(Guid UserAwid);
public sealed record UpdatePreferencesRequest(Guid UserAwid, bool? PushEnabled, bool? EmailEnabled, bool? InAppEnabled, bool? MarketingEnabled, bool? SecurityAlertsEnabled, bool? PaymentAlertsEnabled, string? Language);
