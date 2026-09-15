namespace AfriWallet.PaymentRequests.Application;

public sealed record PaymentRequestEventOutboxDiagnosticsSnapshot(
    long PendingCount,
    long ProcessingCount,
    long RetryCount,
    long DeliveredCount,
    long DeadLetterCount,
    DateTimeOffset? OldestUndeliveredAtUtc,
    DateTimeOffset? LatestDeadLetterAtUtc);

public interface IPaymentRequestEventOutboxDiagnostics
{
    Task<PaymentRequestEventOutboxDiagnosticsSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default);
}
