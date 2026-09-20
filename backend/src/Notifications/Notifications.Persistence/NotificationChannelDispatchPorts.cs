using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;

namespace AfriWallet.Notifications.Persistence;

public sealed class InAppNotificationChannelDispatchPort(
    IInAppNotificationRepository repository) : INotificationChannelDispatchPort
{
    public NotificationChannel Channel => NotificationChannel.InApp;

    public async Task DispatchAsync(
        NotificationDelivery delivery,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        await repository.AddAsync(ToNotification(delivery), cancellationToken);
    }

    private static InAppNotification ToNotification(NotificationDelivery delivery) =>
        InAppNotification.Restore(
            delivery.DeliveryId,
            delivery.RecipientUserId,
            delivery.EventId,
            delivery.PaymentRequestId,
            delivery.EventKind,
            delivery.CreatedAtUtc,
            delivery.TransferId,
            null,
            null);
}

public sealed class PushNotificationChannelDispatchPort(
    NotificationEventPushDeliveryService pushDeliveryService,
    TimeProvider timeProvider) : INotificationChannelDispatchPort
{
    public NotificationChannel Channel => NotificationChannel.Push;

    public async Task DispatchAsync(
        NotificationDelivery delivery,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        var attemptedAtUtc = timeProvider.GetUtcNow();
        if (attemptedAtUtc.Offset != TimeSpan.Zero)
            throw new InvalidOperationException("TimeProvider must return UTC timestamps.");

        await pushDeliveryService.DeliverAsync(
            InAppNotification.Restore(
                delivery.DeliveryId,
                delivery.RecipientUserId,
                delivery.EventId,
                delivery.PaymentRequestId,
                delivery.EventKind,
                delivery.CreatedAtUtc,
                delivery.TransferId,
                null,
                null),
            attemptedAtUtc,
            cancellationToken);
    }
}
