using AfriWallet.PaymentRequests.Application;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.Persistence;

public sealed class EfPaymentRequestEventAttemptLedger(PaymentRequestDbContext dbContext)
    : IPaymentRequestEventAttemptLedger
{
    public async Task<Guid> BeginAttemptAsync(
        Guid eventId,
        int attemptNumber,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken = default)
    {
        InMemoryPaymentRequestEventAttemptLedger.ValidateBegin(eventId, attemptNumber, startedAtUtc);
        cancellationToken.ThrowIfCancellationRequested();

        var existing = await dbContext.PaymentRequestEventAttempts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.EventId == eventId && x.AttemptNumber == attemptNumber, cancellationToken);
        if (existing is not null)
        {
            return existing.AttemptId;
        }

        var entity = new PaymentRequestEventAttemptEntity
        {
            AttemptId = Guid.NewGuid(),
            EventId = eventId,
            AttemptNumber = attemptNumber,
            StartedAtUtc = startedAtUtc.UtcDateTime
        };

        dbContext.PaymentRequestEventAttempts.Add(entity);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return entity.AttemptId;
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            existing = await dbContext.PaymentRequestEventAttempts.AsNoTracking()
                .SingleOrDefaultAsync(x => x.EventId == eventId && x.AttemptNumber == attemptNumber, cancellationToken);
            if (existing is not null)
            {
                return existing.AttemptId;
            }

            throw;
        }
    }

    public async Task CompleteAttemptAsync(
        Guid attemptId,
        PaymentRequestEventAttemptOutcome outcome,
        DateTimeOffset completedAtUtc,
        string? error = null,
        DateTimeOffset? nextAttemptAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        InMemoryPaymentRequestEventAttemptLedger.ValidateCompletion(
            attemptId, outcome, completedAtUtc, nextAttemptAtUtc);
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedError = InMemoryPaymentRequestEventAttemptLedger.NormalizeError(error);
        var affected = await dbContext.PaymentRequestEventAttempts
            .Where(x => x.AttemptId == attemptId && x.CompletedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.CompletedAtUtc, completedAtUtc.UtcDateTime)
                .SetProperty(x => x.Outcome, (int)outcome)
                .SetProperty(x => x.Error, normalizedError)
                .SetProperty(x => x.NextAttemptAtUtc, nextAttemptAtUtc == null ? null : nextAttemptAtUtc.Value.UtcDateTime),
                cancellationToken);

        if (affected == 1)
        {
            return;
        }

        var existing = await dbContext.PaymentRequestEventAttempts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.AttemptId == attemptId, cancellationToken);
        if (existing is null)
        {
            throw new InvalidOperationException("Attempt was not found.");
        }

        if (existing.CompletedAtUtc is null)
        {
            throw new InvalidOperationException("Attempt completion could not be persisted.");
        }
    }

    public async Task<IReadOnlyList<PaymentRequestEventAttempt>> ListByEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        if (eventId == Guid.Empty) throw new ArgumentException("Event id cannot be empty.", nameof(eventId));
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await dbContext.PaymentRequestEventAttempts.AsNoTracking()
            .Where(x => x.EventId == eventId)
            .OrderBy(x => x.AttemptNumber)
            .ToArrayAsync(cancellationToken);

        return rows.Select(x => new PaymentRequestEventAttempt(
            x.AttemptId,
            x.EventId,
            x.AttemptNumber,
            new DateTimeOffset(DateTime.SpecifyKind(x.StartedAtUtc, DateTimeKind.Utc)),
            x.CompletedAtUtc is null ? null : new DateTimeOffset(DateTime.SpecifyKind(x.CompletedAtUtc.Value, DateTimeKind.Utc)),
            x.Outcome is null ? null : (PaymentRequestEventAttemptOutcome)x.Outcome.Value,
            x.Error,
            x.NextAttemptAtUtc is null ? null : new DateTimeOffset(DateTime.SpecifyKind(x.NextAttemptAtUtc.Value, DateTimeKind.Utc))))
            .ToArray();
    }
}
