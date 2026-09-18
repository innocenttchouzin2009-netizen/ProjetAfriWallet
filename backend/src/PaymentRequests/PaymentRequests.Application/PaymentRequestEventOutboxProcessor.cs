namespace AfriWallet.PaymentRequests.Application;

public sealed class PaymentRequestEventOutboxProcessor
{
    private readonly IPaymentRequestEventOutboxStore outboxStore;
    private readonly IPaymentRequestEventDeliveryPort deliveryPort;
    private readonly IPaymentRequestEventAttemptLedger attemptLedger;
    private readonly PaymentRequestEventDeliveryOptions options;

    public PaymentRequestEventOutboxProcessor(
        IPaymentRequestEventOutboxStore outboxStore,
        IPaymentRequestEventDeliveryPort deliveryPort,
        PaymentRequestEventDeliveryOptions? options = null)
        : this(outboxStore, deliveryPort, new InMemoryPaymentRequestEventAttemptLedger(), options)
    {
    }

    public PaymentRequestEventOutboxProcessor(
        IPaymentRequestEventOutboxStore outboxStore,
        IPaymentRequestEventDeliveryPort deliveryPort,
        IPaymentRequestEventAttemptLedger attemptLedger,
        PaymentRequestEventDeliveryOptions? options = null)
    {
        this.outboxStore = outboxStore ?? throw new ArgumentNullException(nameof(outboxStore));
        this.deliveryPort = deliveryPort ?? throw new ArgumentNullException(nameof(deliveryPort));
        this.attemptLedger = attemptLedger ?? throw new ArgumentNullException(nameof(attemptLedger));
        this.options = options ?? PaymentRequestEventDeliveryOptions.Default;
    }

    public async Task<int> ProcessBatchAsync(
        int maxCount,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        if (maxCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCount), "Batch size must be positive.");
        }

        EnsureUtc(nowUtc, nameof(nowUtc));
        ValidateOptions();
        cancellationToken.ThrowIfCancellationRequested();

        await outboxStore.RecoverExpiredClaimsAsync(nowUtc, cancellationToken);
        var claimed = await outboxStore.ClaimBatchAsync(maxCount, nowUtc, options.LeaseDuration, cancellationToken);
        var delivered = 0;

        foreach (var item in claimed)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var attemptId = await attemptLedger.BeginAttemptAsync(
                item.Event.EventId,
                item.AttemptCount,
                nowUtc,
                cancellationToken);

            try
            {
                await deliveryPort.DeliverAsync(item.Event, cancellationToken);
                await attemptLedger.CompleteAttemptAsync(
                    attemptId,
                    PaymentRequestEventAttemptOutcome.Delivered,
                    nowUtc,
                    cancellationToken: cancellationToken);
                await outboxStore.MarkDeliveredAsync(item.Event.EventId, nowUtc, cancellationToken);
                delivered++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await attemptLedger.CompleteAttemptAsync(
                    attemptId,
                    PaymentRequestEventAttemptOutcome.Cancelled,
                    nowUtc,
                    cancellationToken: CancellationToken.None);
                throw;
            }
            catch (PaymentRequestEventDeliveryException exception)
            {
                var permanent = exception.FailureKind == PaymentRequestEventDeliveryFailureKind.Permanent;
                await MarkFailureAsync(item, attemptId, nowUtc, exception.Message, permanent, cancellationToken);
            }
            catch (Exception exception)
            {
                await MarkFailureAsync(item, attemptId, nowUtc, exception.Message, permanent: false, cancellationToken);
            }
        }

        return delivered;
    }

    private async Task MarkFailureAsync(
        PaymentRequestEventOutboxItem item,
        Guid attemptId,
        DateTimeOffset nowUtc,
        string error,
        bool permanent,
        CancellationToken cancellationToken)
    {
        var deadLetter = permanent || item.AttemptCount >= options.MaxAttempts;
        DateTimeOffset? nextAttempt = deadLetter
            ? null
            : nowUtc.Add(ComputeRetryDelay(item.AttemptCount));

        await attemptLedger.CompleteAttemptAsync(
            attemptId,
            deadLetter
                ? PaymentRequestEventAttemptOutcome.DeadLetter
                : PaymentRequestEventAttemptOutcome.RetryScheduled,
            nowUtc,
            error,
            nextAttempt,
            cancellationToken);

        await outboxStore.MarkFailedAsync(
            item.Event.EventId,
            nowUtc,
            error,
            nextAttempt,
            deadLetter,
            cancellationToken);
    }

    private TimeSpan ComputeRetryDelay(int attemptCount)
    {
        var exponent = Math.Clamp(attemptCount - 1, 0, 10);
        var multiplier = 1L << exponent;
        var ticks = Math.Min(options.BaseRetryDelay.Ticks * multiplier, TimeSpan.FromHours(24).Ticks);
        return TimeSpan.FromTicks(ticks);
    }

    private void ValidateOptions()
    {
        if (options.MaxAttempts <= 0) throw new InvalidOperationException("MaxAttempts must be positive.");
        if (options.LeaseDuration <= TimeSpan.Zero) throw new InvalidOperationException("LeaseDuration must be positive.");
        if (options.BaseRetryDelay <= TimeSpan.Zero) throw new InvalidOperationException("BaseRetryDelay must be positive.");
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }
    }
}
