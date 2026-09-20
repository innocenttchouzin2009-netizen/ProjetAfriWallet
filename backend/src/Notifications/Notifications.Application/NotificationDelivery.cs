namespace AfriWallet.Notifications.Application;

public enum NotificationDeliveryStatus
{
    Pending = 1,
    Dispatched = 2
}

public sealed record NotificationDelivery(
    Guid DeliveryId,
    Guid EventId,
    NotificationChannel Channel,
    Guid RecipientUserId,
    Guid PaymentRequestId,
    string EventType,
    long AmountMinor,
    string CurrencyCode,
    string Title,
    string Body,
    DateTimeOffset CreatedAtUtc,
    NotificationDeliveryStatus Status,
    DateTimeOffset? DispatchedAtUtc)
{
    public static NotificationDelivery From(InAppNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (notification.SourceEventId == Guid.Empty)
            throw new ArgumentException("Source event id cannot be empty.", nameof(notification));
        if (notification.RecipientUserId == Guid.Empty)
            throw new ArgumentException("Recipient user id cannot be empty.", nameof(notification));
        if (notification.PaymentRequestId == Guid.Empty)
            throw new ArgumentException("Payment request id cannot be empty.", nameof(notification));
        if (notification.CreatedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Notification timestamp must be UTC.", nameof(notification));
        if (notification.AmountMinor <= 0 ||
            string.IsNullOrWhiteSpace(notification.EventType) ||
            string.IsNullOrWhiteSpace(notification.CurrencyCode) ||
            string.IsNullOrWhiteSpace(notification.Title) ||
            string.IsNullOrWhiteSpace(notification.Body))
            throw new ArgumentException("Notification content is invalid.", nameof(notification));

        return new NotificationDelivery(
            Guid.NewGuid(),
            notification.SourceEventId,
            notification.Channel,
            notification.RecipientUserId,
            notification.PaymentRequestId,
            notification.EventType,
            notification.AmountMinor,
            notification.CurrencyCode,
            notification.Title,
            notification.Body,
            notification.CreatedAtUtc,
            NotificationDeliveryStatus.Pending,
            null);
    }
}

public interface INotificationDeliveryRepository
{
    Task<NotificationDelivery?> GetAsync(
        Guid eventId,
        NotificationChannel channel,
        CancellationToken cancellationToken = default);

    Task<NotificationDelivery> GetOrAddAsync(
        NotificationDelivery delivery,
        CancellationToken cancellationToken = default);

    Task MarkDispatchedAsync(
        Guid deliveryId,
        DateTimeOffset dispatchedAtUtc,
        CancellationToken cancellationToken = default);
}

public interface INotificationDispatchPort
{
    NotificationChannel Channel { get; }

    Task DispatchAsync(
        NotificationDelivery delivery,
        CancellationToken cancellationToken = default);
}

public sealed class PersistentNotificationDeliveryService(
    INotificationDeliveryRepository repository,
    IEnumerable<INotificationDispatchPort> dispatchPorts,
    TimeProvider timeProvider)
    : INotificationDeliveryPort
{
    public async Task DeliverAsync(
        InAppNotification notification,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var candidate = NotificationDelivery.From(notification);
        var delivery = await repository.GetOrAddAsync(candidate, cancellationToken);

        if (delivery.Status == NotificationDeliveryStatus.Dispatched)
            return;

        var dispatcher = dispatchPorts.SingleOrDefault(x => x.Channel == delivery.Channel)
            ?? throw new InvalidOperationException(
                $"No notification dispatcher is configured for channel '{delivery.Channel}'.");

        await dispatcher.DispatchAsync(delivery, cancellationToken);

        var dispatchedAtUtc = timeProvider.GetUtcNow();
        await repository.MarkDispatchedAsync(delivery.DeliveryId, dispatchedAtUtc, cancellationToken);
    }
}
