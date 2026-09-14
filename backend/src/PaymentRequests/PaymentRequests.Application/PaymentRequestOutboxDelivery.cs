namespace AfriWallet.PaymentRequests.Application;

public sealed record PaymentRequestOutboxDeliveryMessage(
    Guid MessageId,
    Guid PaymentRequestId,
    string EventType,
    string PayloadJson,
    DateTimeOffset OccurredAtUtc);

public interface IPaymentRequestOutboxStore
{
    Task<IReadOnlyList<PaymentRequestOutboxDeliveryMessage>> ReadPendingAsync(
        int maxCount,
        CancellationToken cancellationToken = default);

    Task MarkPublishedAsync(
        Guid messageId,
        DateTimeOffset publishedAtUtc,
        CancellationToken cancellationToken = default);
}

public interface IPaymentRequestOutboxTransport
{
    Task DeliverAsync(
        PaymentRequestOutboxDeliveryMessage message,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed record PaymentRequestOutboxDispatchOptions(
    int BatchSize = 50,
    int MaxDeliveryAttempts = 3)
{
    public void Validate()
    {
        if (BatchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(BatchSize), "Batch size must be positive.");
        }

        if (MaxDeliveryAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxDeliveryAttempts), "Max delivery attempts must be positive.");
        }
    }
}

public sealed record PaymentRequestOutboxDispatchResult(
    int PendingRead,
    int Published,
    int Failed);

public sealed class PaymentRequestOutboxDispatcher(
    IPaymentRequestOutboxStore store,
    IPaymentRequestOutboxTransport transport,
    TimeProvider timeProvider,
    PaymentRequestOutboxDispatchOptions options)
{
    public async Task<PaymentRequestOutboxDispatchResult> DispatchAsync(
        CancellationToken cancellationToken = default)
    {
        options.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        var pending = await store.ReadPendingAsync(options.BatchSize, cancellationToken);
        var published = 0;
        var failed = 0;

        foreach (var message in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var idempotencyKey = message.MessageId.ToString("N");
            var delivered = false;

            for (var attempt = 1; attempt <= options.MaxDeliveryAttempts; attempt++)
            {
                try
                {
                    await transport.DeliverAsync(message, idempotencyKey, cancellationToken);
                    delivered = true;
                    break;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch when (attempt < options.MaxDeliveryAttempts)
                {
                    // Retry the same message with the same idempotency key.
                }
                catch
                {
                    break;
                }
            }

            if (!delivered)
            {
                failed++;
                continue;
            }

            var publishedAtUtc = timeProvider.GetUtcNow();
            await store.MarkPublishedAsync(message.MessageId, publishedAtUtc, cancellationToken);
            published++;
        }

        return new PaymentRequestOutboxDispatchResult(pending.Count, published, failed);
    }
}
