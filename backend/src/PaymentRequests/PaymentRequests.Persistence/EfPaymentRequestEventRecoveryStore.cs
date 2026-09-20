using AfriWallet.PaymentRequests.Application;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.Persistence;

public sealed class EfPaymentRequestEventRecoveryStore(PaymentRequestDbContext dbContext)
    : IPaymentRequestEventRecoveryStore
{
    private const string RecoveryError =
        "Recovered after expired processing lease following worker interruption or process restart.";

    public async Task<int> RecoverExpiredClaimsAsync(
        int maxCount,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        if (maxCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxCount), "Recovery batch size must be positive.");
        if (nowUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Recovery timestamp must be UTC.", nameof(nowUtc));

        cancellationToken.ThrowIfCancellationRequested();
        var now = nowUtc.UtcDateTime;

        var candidateIds = await dbContext.PaymentRequestEventOutbox.AsNoTracking()
            .Where(x =>
                x.Status == (int)PaymentRequestEventOutboxStatus.Processing &&
                x.LeaseExpiresAtUtc != null &&
                x.LeaseExpiresAtUtc <= now)
            .OrderBy(x => x.LeaseExpiresAtUtc)
            .ThenBy(x => x.EnqueuedAtUtc)
            .ThenBy(x => x.EventId)
            .Select(x => x.EventId)
            .Take(maxCount)
            .ToArrayAsync(cancellationToken);

        var recovered = 0;

        foreach (var eventId in candidateIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var transaction =
                await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var row = await dbContext.PaymentRequestEventOutbox.AsNoTracking()
                    .SingleOrDefaultAsync(
                        x => x.EventId == eventId &&
                             x.Status == (int)PaymentRequestEventOutboxStatus.Processing &&
                             x.LeaseExpiresAtUtc != null &&
                             x.LeaseExpiresAtUtc <= now,
                        cancellationToken);

                if (row is null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    continue;
                }

                if (row.AttemptCount <= 0)
                    throw new InvalidOperationException("Processing Outbox row has an invalid attempt count.");

                var transitioned = await dbContext.PaymentRequestEventOutbox
                    .Where(x =>
                        x.EventId == eventId &&
                        x.Status == (int)PaymentRequestEventOutboxStatus.Processing &&
                        x.LeaseExpiresAtUtc != null &&
                        x.LeaseExpiresAtUtc <= now)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.Status, (int)PaymentRequestEventOutboxStatus.Retry)
                        .SetProperty(x => x.AvailableAtUtc, now)
                        .SetProperty(x => x.LeaseToken, (Guid?)null)
                        .SetProperty(x => x.LeaseExpiresAtUtc, (DateTime?)null)
                        .SetProperty(x => x.LastError, RecoveryError),
                        cancellationToken);

                if (transitioned != 1)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    continue;
                }

                var attempt = await dbContext.PaymentRequestEventAttempts.AsNoTracking()
                    .SingleOrDefaultAsync(
                        x => x.EventId == eventId &&
                             x.AttemptNumber == row.AttemptCount,
                        cancellationToken);

                if (attempt is null)
                {
                    dbContext.PaymentRequestEventAttempts.Add(
                        new PaymentRequestEventAttemptEntity
                        {
                            AttemptId = Guid.NewGuid(),
                            EventId = eventId,
                            AttemptNumber = row.AttemptCount,
                            StartedAtUtc = row.LastAttemptAtUtc ?? row.EnqueuedAtUtc,
                            CompletedAtUtc = now,
                            Outcome = (int)PaymentRequestEventAttemptOutcome.RetryScheduled,
                            Error = RecoveryError,
                            NextAttemptAtUtc = now
                        });
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
                else if (attempt.CompletedAtUtc is null)
                {
                    var completed = await dbContext.PaymentRequestEventAttempts
                        .Where(x =>
                            x.AttemptId == attempt.AttemptId &&
                            x.CompletedAtUtc == null)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(x => x.CompletedAtUtc, now)
                            .SetProperty(x => x.Outcome, (int)PaymentRequestEventAttemptOutcome.RetryScheduled)
                            .SetProperty(x => x.Error, RecoveryError)
                            .SetProperty(x => x.NextAttemptAtUtc, now),
                            cancellationToken);

                    if (completed != 1)
                        throw new InvalidOperationException(
                            "Expired attempt could not be finalized during recovery.");
                }

                await transaction.CommitAsync(cancellationToken);
                dbContext.ChangeTracker.Clear();
                recovered++;
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                dbContext.ChangeTracker.Clear();
                throw;
            }
        }

        return recovered;
    }
}
