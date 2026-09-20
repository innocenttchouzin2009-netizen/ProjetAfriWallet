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
    public static NotificationDelivery From(InAppNotification notification) =>
        From(notification, notification.Channel);

    public static NotificationDelivery From(
        InAppNotification notification,
        NotificationChannel channel)
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
        if (!Enum.IsDefined(channel))
            throw new ArgumentOutOfRangeException(nameof(channel));
        if (notification.AmountMinor <= 0 ||
            string.IsNullOrWhiteSpace(notification.EventType) ||
            string.IsNullOrWhiteSpace(notification.CurrencyCode) ||
            string.IsNullOrWhiteSpace(notification.Title) ||
            string.IsNullOrWhiteSpace(notification.Body))
            throw new ArgumentException("Notification content is invalid.", nameof(notification));

        return new NotificationDelivery(
            Guid.NewGuid(),
            notification.SourceEventId,
            channel,
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

public sealed class NotificationDeliveryFanoutService(
    INotificationDeliveryRepository repository,
    IEnumerable<INotificationDispatchPort> dispatchPorts,
    TimeProvider timeProvider)
{
    public async Task DispatchAsync(
        InAppNotification notification,
        IReadOnlyCollection<NotificationChannel> channels,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(channels);

        if (channels.Count == 0)
            throw new ArgumentException("At least one notification channel is required.", nameof(channels));

        cancellationToken.ThrowIfCancellationRequested();

        var requestedChannels = channels.ToArray();
        if (requestedChannels.Any(channel => !Enum.IsDefined(channel)))
            throw new ArgumentOutOfRangeException(nameof(channels), "All notification channels must be defined.");

        if (requestedChannels.Distinct().Count() != requestedChannels.Length)
            throw new ArgumentException("Notification channels must be unique.", nameof(channels));

        var dispatcherMap = dispatchPorts
            .GroupBy(dispatcher => dispatcher.Channel)
            .ToDictionary(group => group.Key, group => group.ToArray());

        foreach (var channel in requestedChannels)
        {
            if (!dispatcherMap.TryGetValue(channel, out var matches) || matches.Length == 0)
                throw new InvalidOperationException(
                    $"No notification dispatcher is configured for channel '{channel}'.");

            if (matches.Length != 1)
                throw new InvalidOperationException(
                    $"Multiple notification dispatchers are configured for channel '{channel}'.");
        }

        foreach (var channel in requestedChannels)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidate = NotificationDelivery.From(notification, channel);
            var delivery = await repository.GetOrAddAsync(candidate, cancellationToken);

            if (delivery.Status == NotificationDeliveryStatus.Dispatched)
                continue;

            await dispatcherMap[channel][0].DispatchAsync(delivery, cancellationToken);

            var dispatchedAtUtc = timeProvider.GetUtcNow();
            if (dispatchedAtUtc.Offset != TimeSpan.Zero)
                throw new InvalidOperationException("TimeProvider must return UTC timestamps.");

            await repository.MarkDispatchedAsync(delivery.DeliveryId, dispatchedAtUtc, cancellationToken);
        }
    }
}

public sealed class PersistentNotificationDeliveryService(
    NotificationDeliveryFanoutService fanoutService)
    : INotificationDeliveryPort
{
    public Task DeliverAsync(
        InAppNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return fanoutService.DispatchAsync(
            notification,
            new[] { notification.Channel },
            cancellationToken);
    }
}
