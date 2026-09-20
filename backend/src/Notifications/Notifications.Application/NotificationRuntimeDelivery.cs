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
        Guid recipientUserId,
        CancellationToken cancellationToken = default);

    Task<NotificationDelivery> GetOrAddAsync(
        NotificationDelivery delivery,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDelivery>> ClaimRecoverableAsync(
        DateTimeOffset nowUtc,
        DateTimeOffset leaseUntilUtc,
        int limit,
        CancellationToken cancellationToken = default);

    Task ReleaseRecoveryClaimAsync(
        Guid deliveryId,
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

public sealed record NotificationDeliveryRecoveryOptions(TimeSpan LeaseDuration, int BatchSize)
{
    public static NotificationDeliveryRecoveryOptions Default { get; } =
        new(TimeSpan.FromMinutes(2), 100);

    public NotificationDeliveryRecoveryOptions Validate()
    {
        if (LeaseDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(LeaseDuration));
        if (BatchSize is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(BatchSize));
        return this;
    }
}

public sealed record NotificationDeliveryRecoveryResult(
    int Claimed,
    int Dispatched,
    int Failed);

public sealed class NotificationDeliveryRecoveryService(
    INotificationDeliveryRepository deliveryRepository,
    IEnumerable<INotificationChannelDispatchPort> dispatchPorts,
    TimeProvider timeProvider,
    NotificationDeliveryRecoveryOptions options)
{
    public async Task<NotificationDeliveryRecoveryResult> RecoverAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validated = options.Validate();
        var nowUtc = timeProvider.GetUtcNow();
        if (nowUtc.Offset != TimeSpan.Zero)
            throw new InvalidOperationException("TimeProvider must return UTC timestamps.");

        var leaseUntilUtc = nowUtc.Add(validated.LeaseDuration);
        var claimed = await deliveryRepository.ClaimRecoverableAsync(
            nowUtc,
            leaseUntilUtc,
            validated.BatchSize,
            cancellationToken);

        var dispatcherMap = dispatchPorts
            .GroupBy(x => x.Channel)
            .ToDictionary(x => x.Key, x => x.ToArray());

        var dispatched = 0;
        var failed = 0;

        foreach (var delivery in claimed)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!dispatcherMap.TryGetValue(delivery.Channel, out var matches) || matches.Length != 1)
            {
                await deliveryRepository.ReleaseRecoveryClaimAsync(
                    delivery.DeliveryId,
                    CancellationToken.None);
                failed++;
                continue;
            }

            try
            {
                await matches[0].DispatchAsync(delivery, cancellationToken);

                var dispatchedAtUtc = timeProvider.GetUtcNow();
                if (dispatchedAtUtc.Offset != TimeSpan.Zero)
                    throw new InvalidOperationException("TimeProvider must return UTC timestamps.");

                await deliveryRepository.MarkDispatchedAsync(
                    delivery.DeliveryId,
                    dispatchedAtUtc,
                    cancellationToken);
                dispatched++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await deliveryRepository.ReleaseRecoveryClaimAsync(
                    delivery.DeliveryId,
                    CancellationToken.None);
                throw;
            }
            catch
            {
                await deliveryRepository.ReleaseRecoveryClaimAsync(
                    delivery.DeliveryId,
                    CancellationToken.None);
                failed++;
            }
        }

        return new NotificationDeliveryRecoveryResult(claimed.Count, dispatched, failed);
    }
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
