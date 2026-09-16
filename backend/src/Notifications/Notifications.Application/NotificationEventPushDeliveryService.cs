using AfriWallet.Notifications.Domain;

namespace AfriWallet.Notifications.Application;

public enum PushEventDeliveryState
{
    Delivered = 1,
    RetryScheduled = 2,
    InvalidToken = 3,
    PermanentFailure = 4,
    RetryExhausted = 5
}

public sealed record PushEventDeliveryRecord(
    Guid EventId,
    Guid UserId,
    PushDeviceRegistrationId RegistrationId,
    int AttemptCount,
    PushEventDeliveryState State,
    DateTimeOffset LastAttemptAtUtc,
    DateTimeOffset? NextAttemptAtUtc);

public interface IPushEventDeliveryRepository
{
    Task<IReadOnlyList<PushEventDeliveryRecord>> ListByEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task UpsertAsync(PushEventDeliveryRecord record, CancellationToken cancellationToken = default);
}

public sealed record PushEventRetryPolicy
{
    public PushEventRetryPolicy(int maxAttempts, IReadOnlyList<TimeSpan> retryDelays)
    {
        if (maxAttempts < 1) throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        ArgumentNullException.ThrowIfNull(retryDelays);
        if (retryDelays.Any(delay => delay <= TimeSpan.Zero))
            throw new ArgumentException("Retry delays must be positive.", nameof(retryDelays));
        if (retryDelays.Count < maxAttempts - 1)
            throw new ArgumentException("Retry delays must cover every retry attempt.", nameof(retryDelays));
        MaxAttempts = maxAttempts;
        RetryDelays = retryDelays;
    }

    public int MaxAttempts { get; }
    public IReadOnlyList<TimeSpan> RetryDelays { get; }
    public static PushEventRetryPolicy Default { get; } = new(4, [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30)]);
}

public sealed record PushEventDeliveryResult(Guid EventId, int AttemptedTargets, int Delivered, int RetryScheduled, int TerminalFailures);

public sealed class NotificationEventPushDeliveryService(
    PushDeliveryOrchestrationService deliveryService,
    IPushEventDeliveryRepository deliveryRepository,
    PushEventRetryPolicy retryPolicy)
{
    public async Task<PushEventDeliveryResult> DeliverAsync(
        InAppNotification notification,
        DateTimeOffset attemptedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (attemptedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Attempt timestamp must be UTC.", nameof(attemptedAtUtc));
        cancellationToken.ThrowIfCancellationRequested();

        var existing = await deliveryRepository.ListByEventAsync(notification.EventId, cancellationToken);
        if (existing.Any(record => record.UserId != notification.UserId))
            throw new InvalidOperationException("Notification event delivery user does not match existing delivery records.");

        IReadOnlySet<Guid>? included = null;
        if (existing.Count > 0)
        {
            included = existing
                .Where(record => record.State == PushEventDeliveryState.RetryScheduled && record.NextAttemptAtUtc <= attemptedAtUtc)
                .Select(record => record.RegistrationId.Value)
                .ToHashSet();
        }

        var batch = await deliveryService.DeliverToUserAsync(
            notification.UserId,
            CreateMessage(notification),
            attemptedAtUtc,
            cancellationToken,
            included);

        var delivered = 0;
        var retries = 0;
        var terminal = 0;

        foreach (var outcome in batch.Outcomes)
        {
            var previous = existing.SingleOrDefault(record => record.RegistrationId == outcome.RegistrationId);
            var attemptCount = (previous?.AttemptCount ?? 0) + 1;
            PushEventDeliveryState state;
            DateTimeOffset? nextAttempt = null;

            if (outcome.Accepted)
            {
                state = PushEventDeliveryState.Delivered;
                delivered++;
            }
            else if (outcome.FailureKind == PushDeliveryFailureKind.Transient && attemptCount < retryPolicy.MaxAttempts)
            {
                state = PushEventDeliveryState.RetryScheduled;
                nextAttempt = attemptedAtUtc.Add(retryPolicy.RetryDelays[attemptCount - 1]);
                retries++;
            }
            else
            {
                state = outcome.FailureKind switch
                {
                    PushDeliveryFailureKind.InvalidToken => PushEventDeliveryState.InvalidToken,
                    PushDeliveryFailureKind.Permanent => PushEventDeliveryState.PermanentFailure,
                    PushDeliveryFailureKind.Transient => PushEventDeliveryState.RetryExhausted,
                    _ => PushEventDeliveryState.PermanentFailure
                };
                terminal++;
            }

            await deliveryRepository.UpsertAsync(new PushEventDeliveryRecord(
                notification.EventId,
                notification.UserId,
                outcome.RegistrationId,
                attemptCount,
                state,
                attemptedAtUtc,
                nextAttempt), cancellationToken);
        }

        return new PushEventDeliveryResult(notification.EventId, batch.Outcomes.Count, delivered, retries, terminal);
    }

    private static PushNotificationMessage CreateMessage(InAppNotification notification)
    {
        var (title, body) = notification.EventKind switch
        {
            PaymentRequestEventKind.Created => ("Payment request received", "You received a new payment request."),
            PaymentRequestEventKind.Accepted => ("Payment request accepted", "A payment request was accepted."),
            PaymentRequestEventKind.Declined => ("Payment request declined", "A payment request was declined."),
            PaymentRequestEventKind.Cancelled => ("Payment request cancelled", "A payment request was cancelled."),
            PaymentRequestEventKind.Expired => ("Payment request expired", "A payment request expired."),
            PaymentRequestEventKind.Paid => ("Payment request paid", "A payment request was paid."),
            _ => throw new ArgumentOutOfRangeException()
        };

        var data = new Dictionary<string, string>
        {
            ["eventId"] = notification.EventId.ToString(),
            ["paymentRequestId"] = notification.PaymentRequestId.ToString(),
            ["kind"] = notification.EventKind.ToString()
        };
        if (notification.TransferId is Guid transferId) data["transferId"] = transferId.ToString();
        return PushNotificationMessage.Create(notification.Id, title, body, data);
    }
}
