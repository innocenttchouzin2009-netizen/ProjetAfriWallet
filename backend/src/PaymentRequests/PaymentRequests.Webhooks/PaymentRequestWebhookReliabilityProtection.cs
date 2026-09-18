namespace AfriWallet.PaymentRequests.Webhooks;

public sealed class PaymentRequestWebhookReliabilityProtectionOptions
{
    public PaymentRequestWebhookReliabilityProtectionOptions(
        int minimumAttempts,
        decimal failureRateThreshold,
        int consecutiveFailureThreshold,
        int permanentFailureThreshold)
    {
        if (minimumAttempts < 1) throw new ArgumentOutOfRangeException(nameof(minimumAttempts));
        if (failureRateThreshold is <= 0m or > 1m) throw new ArgumentOutOfRangeException(nameof(failureRateThreshold));
        if (consecutiveFailureThreshold < 1) throw new ArgumentOutOfRangeException(nameof(consecutiveFailureThreshold));
        if (permanentFailureThreshold < 1) throw new ArgumentOutOfRangeException(nameof(permanentFailureThreshold));

        MinimumAttempts = minimumAttempts;
        FailureRateThreshold = failureRateThreshold;
        ConsecutiveFailureThreshold = consecutiveFailureThreshold;
        PermanentFailureThreshold = permanentFailureThreshold;
    }

    public int MinimumAttempts { get; }
    public decimal FailureRateThreshold { get; }
    public int ConsecutiveFailureThreshold { get; }
    public int PermanentFailureThreshold { get; }

    public static PaymentRequestWebhookReliabilityProtectionOptions Default { get; } =
        new(5, 0.80m, 5, 3);
}

public enum PaymentRequestWebhookReliabilityProtectionStatus
{
    NoAction = 1,
    AutoDisabled = 2,
    AlreadyDisabled = 3,
    SubscriptionNotFound = 4
}

public sealed record PaymentRequestWebhookReliabilityProtectionResult(
    PaymentRequestWebhookReliabilityProtectionStatus Status,
    string Reason,
    PaymentRequestWebhookDeliveryReliabilityMetrics? Metrics,
    int ConsecutiveFailureCount);

public interface IPaymentRequestWebhookReliabilityProtector
{
    Task<PaymentRequestWebhookReliabilityProtectionResult> EvaluateAndProtectAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);
}

public sealed class NoOpPaymentRequestWebhookReliabilityProtector
    : IPaymentRequestWebhookReliabilityProtector
{
    public static NoOpPaymentRequestWebhookReliabilityProtector Instance { get; } = new();

    private NoOpPaymentRequestWebhookReliabilityProtector() { }

    public Task<PaymentRequestWebhookReliabilityProtectionResult> EvaluateAndProtectAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new PaymentRequestWebhookReliabilityProtectionResult(
            PaymentRequestWebhookReliabilityProtectionStatus.NoAction,
            "Reliability protection is not configured.",
            null,
            0));
    }
}

public sealed class PaymentRequestWebhookReliabilityProtector(
    IPaymentRequestWebhookSubscriptionRegistry registry,
    IPaymentRequestWebhookDeliveryAttemptStore attemptStore,
    IPaymentRequestWebhookSubscriptionAuditStore auditStore,
    PaymentRequestWebhookReliabilityProtectionOptions options,
    TimeProvider timeProvider)
    : IPaymentRequestWebhookReliabilityProtector
{
    private const string SystemActor = "system:webhook-reliability-policy";

    public async Task<PaymentRequestWebhookReliabilityProtectionResult> EvaluateAndProtectAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Webhook subscription id cannot be empty.", nameof(subscriptionId));

        cancellationToken.ThrowIfCancellationRequested();

        var subscription = await registry.GetAsync(subscriptionId, cancellationToken);
        if (subscription is null)
        {
            return new(
                PaymentRequestWebhookReliabilityProtectionStatus.SubscriptionNotFound,
                "Webhook subscription was not found.",
                null,
                0);
        }

        if (subscription.Status == PaymentRequestWebhookSubscriptionStatus.Disabled)
        {
            return new(
                PaymentRequestWebhookReliabilityProtectionStatus.AlreadyDisabled,
                "Webhook subscription is already disabled.",
                null,
                0);
        }

        var metrics = await attemptStore.GetMetricsAsync(subscriptionId, cancellationToken);
        var recentAttempts = await attemptStore.ListAsync(
            subscriptionId,
            Math.Max(options.ConsecutiveFailureThreshold, 1),
            cancellationToken);

        var consecutiveFailures = recentAttempts
            .TakeWhile(x => x.Outcome != PaymentRequestWebhookDeliveryAttemptOutcome.Success)
            .Count();

        var minimumReached = metrics.AttemptCount >= options.MinimumAttempts;
        var permanentFailureThresholdReached =
            minimumReached &&
            metrics.PermanentFailureCount >= options.PermanentFailureThreshold;
        var sustainedFailureThresholdReached =
            minimumReached &&
            metrics.FailureRate >= options.FailureRateThreshold &&
            consecutiveFailures >= options.ConsecutiveFailureThreshold;

        if (!permanentFailureThresholdReached && !sustainedFailureThresholdReached)
        {
            return new(
                PaymentRequestWebhookReliabilityProtectionStatus.NoAction,
                "Reliability thresholds are not met.",
                metrics,
                consecutiveFailures);
        }

        var reason = permanentFailureThresholdReached
            ? "permanent-failure-threshold"
            : "sustained-failure-rate";

        var atUtc = timeProvider.GetUtcNow();
        subscription.Disable(atUtc);
        await registry.UpdateAsync(subscription, cancellationToken);

        var detail =
            $"Automatic reliability protection disabled webhook subscription; reason={reason}; " +
            $"attempts={metrics.AttemptCount}; failureRate={metrics.FailureRate:0.####}; " +
            $"permanentFailures={metrics.PermanentFailureCount}; consecutiveFailures={consecutiveFailures}; " +
            $"thresholds(minAttempts={options.MinimumAttempts},failureRate={options.FailureRateThreshold:0.####}," +
            $"permanentFailures={options.PermanentFailureThreshold},consecutiveFailures={options.ConsecutiveFailureThreshold}).";

        await auditStore.AppendAsync(
            new PaymentRequestWebhookSubscriptionAuditEntry(
                Guid.NewGuid(),
                subscription.Id,
                subscription.IntegrationId,
                subscription.MerchantId,
                SystemActor,
                PaymentRequestWebhookSubscriptionAuditOperation.ReliabilityAutoDisabled,
                subscription.KeyId,
                subscription.SecretReference,
                true,
                null,
                detail,
                atUtc),
            cancellationToken);

        return new(
            PaymentRequestWebhookReliabilityProtectionStatus.AutoDisabled,
            reason,
            metrics,
            consecutiveFailures);
    }
}
