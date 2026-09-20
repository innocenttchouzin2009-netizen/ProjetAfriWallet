namespace AfriWallet.PaymentRequests.Application;

public interface IPaymentRequestEventAttemptFinalizer
{
    Task FinalizeDeliveredAsync(
        Guid attemptId,
        Guid eventId,
        DateTimeOffset completedAtUtc,
        CancellationToken cancellationToken = default);

    Task FinalizeFailedAsync(
        Guid attemptId,
        Guid eventId,
        DateTimeOffset completedAtUtc,
        string error,
        DateTimeOffset? nextAttemptAtUtc,
        bool deadLetter,
        CancellationToken cancellationToken = default);
}

public sealed class SequentialPaymentRequestEventAttemptFinalizer(
    IPaymentRequestEventAttemptLedger attemptLedger,
    IPaymentRequestEventOutboxStore outboxStore)
    : IPaymentRequestEventAttemptFinalizer
{
    public async Task FinalizeDeliveredAsync(
        Guid attemptId,
        Guid eventId,
        DateTimeOffset completedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await attemptLedger.CompleteAttemptAsync(
            attemptId,
            PaymentRequestEventAttemptOutcome.Delivered,
            completedAtUtc,
            cancellationToken: cancellationToken);

        await outboxStore.MarkDeliveredAsync(eventId, completedAtUtc, cancellationToken);
    }

    public async Task FinalizeFailedAsync(
        Guid attemptId,
        Guid eventId,
        DateTimeOffset completedAtUtc,
        string error,
        DateTimeOffset? nextAttemptAtUtc,
        bool deadLetter,
        CancellationToken cancellationToken = default)
    {
        await attemptLedger.CompleteAttemptAsync(
            attemptId,
            deadLetter
                ? PaymentRequestEventAttemptOutcome.DeadLetter
                : PaymentRequestEventAttemptOutcome.RetryScheduled,
            completedAtUtc,
            error,
            nextAttemptAtUtc,
            cancellationToken);

        await outboxStore.MarkFailedAsync(
            eventId,
            completedAtUtc,
            error,
            nextAttemptAtUtc,
            deadLetter,
            cancellationToken);
    }
}
