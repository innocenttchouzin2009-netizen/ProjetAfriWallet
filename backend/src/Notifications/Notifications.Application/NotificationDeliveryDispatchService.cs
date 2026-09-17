using AfriWallet.Notifications.Domain;

namespace AfriWallet.Notifications.Application;

public interface INotificationPushDispatchPort
{
    Task<PushEventDeliveryResult> DispatchAsync(
        InAppNotification notification,
        DateTimeOffset attemptedAtUtc,
        CancellationToken cancellationToken = default);
}

public sealed record NotificationDeliveryDispatchResult(
    Guid NotificationId,
    bool InAppAdded,
    PushEventDeliveryResult Push);

public sealed class NotificationDeliveryDispatchService(
    IInAppNotificationRepository inAppRepository,
    INotificationPushDispatchPort pushDispatchPort)
{
    public async Task<NotificationDeliveryDispatchResult> DispatchAsync(
        InAppNotification notification,
        DateTimeOffset attemptedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (attemptedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Attempt timestamp must be UTC.", nameof(attemptedAtUtc));

        cancellationToken.ThrowIfCancellationRequested();

        // In-App is the mandatory delivery path. It is persisted before any optional Push dispatch.
        var inAppAdded = await inAppRepository.AddAsync(notification, cancellationToken);
        var push = await pushDispatchPort.DispatchAsync(notification, attemptedAtUtc, cancellationToken);

        return new NotificationDeliveryDispatchResult(notification.Id, inAppAdded, push);
    }
}
