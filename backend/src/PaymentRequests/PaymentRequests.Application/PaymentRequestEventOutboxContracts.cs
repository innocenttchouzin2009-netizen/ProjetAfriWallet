using AfriWallet.PaymentRequests.Domain;

namespace AfriWallet.PaymentRequests.Application;

public enum PaymentRequestEventOutboxStatus
{
    Pending = 1,
    Processing = 2,
    Retry = 3,
    Delivered = 4,
    DeadLetter = 5
}

public sealed record PaymentRequestEventEnvelope(
    Guid EventId,
    PaymentRequestId PaymentRequestId,
    string EventType,
    DateTimeOffset OccurredAtUtc,
    string PayloadJson);

public sealed record PaymentRequestEventOutboxItem(
    PaymentRequestEventEnvelope Event,
    PaymentRequestEventOutboxStatus Status,
    int AttemptCount,
    DateTimeOffset EnqueuedAtUtc,
    DateTimeOffset AvailableAtUtc,
    DateTimeOffset? LeaseExpiresAtUtc,
    DateTimeOffset? LastAttemptAtUtc,
    DateTimeOffset? DeliveredAtUtc,
    string? LastError);

public interface IPaymentRequestEventOutboxStore
{
    Task<bool> EnqueueAsync(
        PaymentRequestEventEnvelope paymentRequestEvent,
        DateTimeOffset enqueuedAtUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentRequestEventOutboxItem>> ClaimBatchAsync(
        int maxCount,
        DateTimeOffset nowUtc,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task MarkDeliveredAsync(
        Guid eventId,
        DateTimeOffset deliveredAtUtc,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        Guid eventId,
        DateTimeOffset failedAtUtc,
        string error,
        DateTimeOffset? nextAttemptAtUtc,
        bool deadLetter,
        CancellationToken cancellationToken = default);

    Task<int> RecoverExpiredClaimsAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);
}

public interface IPaymentRequestEventDeliveryPort
{
    Task DeliverAsync(
        PaymentRequestEventEnvelope paymentRequestEvent,
        CancellationToken cancellationToken = default);
}

public sealed record PaymentRequestEventDeliveryOptions(
    int MaxAttempts,
    TimeSpan LeaseDuration,
    TimeSpan BaseRetryDelay)
{
    public static PaymentRequestEventDeliveryOptions Default { get; } =
        new(5, TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(30));
}
