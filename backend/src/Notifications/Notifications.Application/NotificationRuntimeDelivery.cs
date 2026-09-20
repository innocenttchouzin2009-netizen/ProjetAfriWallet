using AfriWallet.Notifications.Domain;

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
    PaymentRequestEventKind EventKind,
    DateTimeOffset CreatedAtUtc,
    Guid? TransferId,
    NotificationDeliveryStatus Status,
    DateTimeOffset? DispatchedAtUtc)
{
    public static NotificationDelivery From(InAppNotification notification, NotificationChannel channel)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (!Enum.IsDefined(channel)) throw new ArgumentOutOfRangeException(nameof(channel));

        return new NotificationDelivery(
            Guid.NewGuid(),
            notification.EventId,
            channel,
            notification.UserId,
            notification.PaymentRequestId,
            notification.EventKind,
            notification.CreatedAtUtc,
            notification.TransferId,
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

public interface INotificationChannelDispatchPort
{
    NotificationChannel Channel { get; }

    Task DispatchAsync(
        NotificationDelivery delivery,
        CancellationToken cancellationToken = default);
}

public sealed class NotificationRuntimeDeliveryService(
    INotificationDeliveryRepository deliveryRepository,
    INotificationPreferenceRepository preferenceRepository,
    INotificationChannelPolicyProvider policyProvider,
    IEnumerable<INotificationChannelDispatchPort> dispatchPorts,
    TimeProvider timeProvider)
{
    public async Task DeliverAsync(
        InAppNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellationToken.ThrowIfCancellationRequested();

        var policies = policyProvider.List();
        if (policies.Count == 0)
            throw new InvalidOperationException("At least one notification channel policy is required.");

        if (policies.Select(x => x.Channel).Distinct().Count() != policies.Count)
            throw new InvalidOperationException("Notification channel policies must be unique by channel.");

        var enabledChannels = new List<NotificationChannel>();
        foreach (var policy in policies.OrderBy(x => x.Channel))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var preference = await preferenceRepository.GetAsync(
                notification.UserId,
                policy.Channel,
                cancellationToken);

            var enabled = policy.UserConfigurable
                ? preference?.IsEnabled ?? policy.DefaultEnabled
                : policy.DefaultEnabled;

            if (enabled)
                enabledChannels.Add(policy.Channel);
        }

        if (enabledChannels.Count == 0)
            return;

        var dispatcherMap = dispatchPorts
            .GroupBy(x => x.Channel)
            .ToDictionary(x => x.Key, x => x.ToArray());

        foreach (var channel in enabledChannels)
        {
            if (!dispatcherMap.TryGetValue(channel, out var matches) || matches.Length == 0)
                throw new InvalidOperationException($"No dispatcher is configured for enabled notification channel '{channel}'.");
            if (matches.Length != 1)
                throw new InvalidOperationException($"Multiple dispatchers are configured for notification channel '{channel}'.");
        }

        foreach (var channel in enabledChannels)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidate = NotificationDelivery.From(notification, channel);
            var delivery = await deliveryRepository.GetOrAddAsync(candidate, cancellationToken);

            if (delivery.Status == NotificationDeliveryStatus.Dispatched)
                continue;

            await dispatcherMap[channel][0].DispatchAsync(delivery, cancellationToken);

            var dispatchedAtUtc = timeProvider.GetUtcNow();
            if (dispatchedAtUtc.Offset != TimeSpan.Zero)
                throw new InvalidOperationException("TimeProvider must return UTC timestamps.");

            await deliveryRepository.MarkDispatchedAsync(
                delivery.DeliveryId,
                dispatchedAtUtc,
                cancellationToken);
        }
    }
}
