using AfriWallet.PaymentRequests.Application;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.Persistence;

public sealed class EfPaymentRequestEventAttemptFinalizer(PaymentRequestDbContext dbContext)
    : IPaymentRequestEventAttemptFinalizer
{
    public async Task FinalizeDeliveredAsync(
        Guid attemptId,
        Guid eventId,
        DateTimeOffset completedAtUtc,
        CancellationToken cancellationToken = default)
    {
        InMemoryPaymentRequestEventAttemptLedger.ValidateCompletion(
            attemptId,
            PaymentRequestEventAttemptOutcome.Delivered,
            completedAtUtc,
            nextAttemptAtUtc: null);
        ValidateEventId(eventId);
        cancellationToken.ThrowIfCancellationRequested();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await CompleteAttemptRowAsync(
                attemptId,
                PaymentRequestEventAttemptOutcome.Delivered,
                completedAtUtc,
                error: null,
                nextAttemptAtUtc: null,
                cancellationToken);

            var deliveredAt = completedAtUtc.UtcDateTime;
            var affectedOutbox = await dbContext.PaymentRequestEventOutbox
                .Where(x => x.EventId == eventId &&
                            x.Status == (int)PaymentRequestEventOutboxStatus.Processing)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, (int)PaymentRequestEventOutboxStatus.Delivered)
                    .SetProperty(x => x.DeliveredAtUtc, deliveredAt)
                    .SetProperty(x => x.LeaseToken, (Guid?)null)
                    .SetProperty(x => x.LeaseExpiresAtUtc, (DateTime?)null)
                    .SetProperty(x => x.LastError, (string?)null),
                    cancellationToken);

            EnsureSingleOutboxTransition(affectedOutbox);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
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
        var outcome = deadLetter
            ? PaymentRequestEventAttemptOutcome.DeadLetter
            : PaymentRequestEventAttemptOutcome.RetryScheduled;

        InMemoryPaymentRequestEventAttemptLedger.ValidateCompletion(
            attemptId,
            outcome,
            completedAtUtc,
            nextAttemptAtUtc);
        ValidateEventId(eventId);

        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Failure error is required.", nameof(error));
        }

        if (!deadLetter && nextAttemptAtUtc is null)
        {
            throw new ArgumentException("Retry time is required for retryable failures.", nameof(nextAttemptAtUtc));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var normalizedError = InMemoryPaymentRequestEventAttemptLedger.NormalizeError(error)!;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await CompleteAttemptRowAsync(
                attemptId,
                outcome,
                completedAtUtc,
                normalizedError,
                nextAttemptAtUtc,
                cancellationToken);

            var failedAt = completedAtUtc.UtcDateTime;
            var availableAt = deadLetter
                ? failedAt
                : nextAttemptAtUtc!.Value.UtcDateTime;
            var status = deadLetter
                ? PaymentRequestEventOutboxStatus.DeadLetter
                : PaymentRequestEventOutboxStatus.Retry;

            var affectedOutbox = await dbContext.PaymentRequestEventOutbox
                .Where(x => x.EventId == eventId &&
                            x.Status == (int)PaymentRequestEventOutboxStatus.Processing)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, (int)status)
                    .SetProperty(x => x.AvailableAtUtc, availableAt)
                    .SetProperty(x => x.LeaseToken, (Guid?)null)
                    .SetProperty(x => x.LeaseExpiresAtUtc, (DateTime?)null)
                    .SetProperty(x => x.LastAttemptAtUtc, failedAt)
                    .SetProperty(x => x.LastError, normalizedError),
                    cancellationToken);

            EnsureSingleOutboxTransition(affectedOutbox);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task CompleteAttemptRowAsync(
        Guid attemptId,
        PaymentRequestEventAttemptOutcome outcome,
        DateTimeOffset completedAtUtc,
        string? error,
        DateTimeOffset? nextAttemptAtUtc,
        CancellationToken cancellationToken)
    {
        var affectedAttempt = await dbContext.PaymentRequestEventAttempts
            .Where(x => x.AttemptId == attemptId && x.CompletedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.CompletedAtUtc, completedAtUtc.UtcDateTime)
                .SetProperty(x => x.Outcome, (int)outcome)
                .SetProperty(x => x.Error, error)
                .SetProperty(
                    x => x.NextAttemptAtUtc,
                    nextAttemptAtUtc == null ? null : nextAttemptAtUtc.Value.UtcDateTime),
                cancellationToken);

        if (affectedAttempt != 1)
        {
            throw new InvalidOperationException("Attempt is not open for finalization.");
        }
    }

    private static void ValidateEventId(Guid eventId)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event id cannot be empty.", nameof(eventId));
        }
    }

    private static void EnsureSingleOutboxTransition(int affected)
    {
        if (affected != 1)
        {
            throw new InvalidOperationException("Outbox event is not currently processing.");
        }
    }
}
