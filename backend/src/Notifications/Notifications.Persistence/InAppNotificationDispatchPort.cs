using AfriWallet.Notifications.Application;

namespace AfriWallet.Notifications.Persistence;

public sealed class InAppNotificationDispatchPort(EfInAppNotificationStore store)
    : INotificationDispatchPort
{
    public NotificationChannel Channel => NotificationChannel.InApp;

    public Task DispatchAsync(
        NotificationDelivery delivery,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        if (delivery.Channel != NotificationChannel.InApp)
            throw new InvalidOperationException("In-App dispatcher received a non In-App delivery.");

        return store.DeliverAsync(
            new InAppNotification(
                delivery.EventId,
                delivery.EventId,
                delivery.RecipientUserId,
                delivery.PaymentRequestId,
                delivery.EventType,
                delivery.AmountMinor,
                delivery.CurrencyCode,
                delivery.Title,
                delivery.Body,
                delivery.CreatedAtUtc,
                NotificationChannel.InApp),
            cancellationToken);
    }
}
