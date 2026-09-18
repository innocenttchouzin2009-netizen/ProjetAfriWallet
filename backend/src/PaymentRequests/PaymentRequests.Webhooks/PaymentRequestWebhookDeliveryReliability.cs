namespace AfriWallet.PaymentRequests.Webhooks;

public enum PaymentRequestWebhookDeliveryAttemptOutcome
{
    Success = 1,
    TransientFailure = 2,
    PermanentFailure = 3
}

public sealed record PaymentRequestWebhookDeliveryAttempt(
    Guid Id,
    Guid SubscriptionId,
    Guid EventId,
    PaymentRequestWebhookDeliveryAttemptOutcome Outcome,
    int? HttpStatusCode,
    long LatencyMilliseconds,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc)
{
    public static PaymentRequestWebhookDeliveryAttempt Create(
        Guid subscriptionId,
        Guid eventId,
        PaymentRequestWebhookDeliveryAttemptOutcome outcome,
        int? httpStatusCode,
        long latencyMilliseconds,
        DateTimeOffset startedAtUtc,
        DateTimeOffset completedAtUtc)
    {
        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Webhook subscription id cannot be empty.", nameof(subscriptionId));
        if (eventId == Guid.Empty)
            throw new ArgumentException("Webhook event id cannot be empty.", nameof(eventId));
        if (!Enum.IsDefined(outcome))
            throw new ArgumentOutOfRangeException(nameof(outcome));
        if (httpStatusCode is < 100 or > 599)
            throw new ArgumentOutOfRangeException(nameof(httpStatusCode));
        if (latencyMilliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(latencyMilliseconds));
        if (startedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Started timestamp must be UTC.", nameof(startedAtUtc));
        if (completedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Completed timestamp must be UTC.", nameof(completedAtUtc));
        if (completedAtUtc < startedAtUtc)
            throw new ArgumentException("Completed timestamp cannot precede started timestamp.", nameof(completedAtUtc));

        return new PaymentRequestWebhookDeliveryAttempt(
            Guid.NewGuid(),
            subscriptionId,
            eventId,
            outcome,
            httpStatusCode,
            latencyMilliseconds,
            startedAtUtc,
            completedAtUtc);
    }
}

public sealed record PaymentRequestWebhookDeliveryReliabilityMetrics(
    Guid SubscriptionId,
    int AttemptCount,
    int SuccessfulAttemptCount,
    int TransientFailureCount,
    int PermanentFailureCount,
    decimal FailureRate,
    DateTimeOffset? LastAttemptAtUtc,
    DateTimeOffset? LastSuccessfulDeliveryAtUtc,
    double? AverageLatencyMilliseconds)
{
    public static PaymentRequestWebhookDeliveryReliabilityMetrics Empty(Guid subscriptionId) =>
        new(subscriptionId, 0, 0, 0, 0, 0m, null, null, null);
}

public interface IPaymentRequestWebhookDeliveryAttemptStore
{
    Task AppendAsync(
        PaymentRequestWebhookDeliveryAttempt attempt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentRequestWebhookDeliveryAttempt>> ListAsync(
        Guid subscriptionId,
        int limit = 100,
        CancellationToken cancellationToken = default);

    Task<PaymentRequestWebhookDeliveryReliabilityMetrics> GetMetricsAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);
}
