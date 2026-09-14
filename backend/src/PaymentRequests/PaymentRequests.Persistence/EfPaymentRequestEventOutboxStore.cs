using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.Persistence;

public sealed class EfPaymentRequestEventOutboxStore(PaymentRequestDbContext dbContext)
    : IPaymentRequestEventOutboxStore
{
    public async Task<bool> EnqueueAsync(
        PaymentRequestEventEnvelope paymentRequestEvent,
        DateTimeOffset enqueuedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paymentRequestEvent);
        ValidateEvent(paymentRequestEvent);
        EnsureUtc(enqueuedAtUtc, nameof(enqueuedAtUtc));
        cancellationToken.ThrowIfCancellationRequested();

        if (await dbContext.PaymentRequestEventOutbox.AsNoTracking()
            .AnyAsync(x => x.EventId == paymentRequestEvent.EventId, cancellationToken))
        {
            return false;
        }

        dbContext.PaymentRequestEventOutbox.Add(new PaymentRequestEventOutboxEntity
        {
            EventId = paymentRequestEvent.EventId,
            PaymentRequestId = paymentRequestEvent.PaymentRequestId.Value,
            EventType = paymentRequestEvent.EventType.Trim(),
            PayloadJson = paymentRequestEvent.PayloadJson,
            OccurredAtUtc = ToUtcDateTime(paymentRequestEvent.OccurredAtUtc),
            EnqueuedAtUtc = ToUtcDateTime(enqueuedAtUtc),
            AvailableAtUtc = ToUtcDateTime(enqueuedAtUtc),
            Status = (int)PaymentRequestEventOutboxStatus.Pending,
            AttemptCount = 0
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            if (await dbContext.PaymentRequestEventOutbox.AsNoTracking()
                .AnyAsync(x => x.EventId == paymentRequestEvent.EventId, cancellationToken))
            {
                return false;
            }

            throw;
        }
    }

    public async Task<IReadOnlyList<PaymentRequestEventOutboxItem>> ClaimBatchAsync(
        int maxCount,
        DateTimeOffset nowUtc,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        if (maxCount <= 0) throw new ArgumentOutOfRangeException(nameof(maxCount));
        if (leaseDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        EnsureUtc(nowUtc, nameof(nowUtc));
        cancellationToken.ThrowIfCancellationRequested();

        var now = ToUtcDateTime(nowUtc);
        var eligibleStatuses = new[]
        {
            (int)PaymentRequestEventOutboxStatus.Pending,
            (int)PaymentRequestEventOutboxStatus.Retry
        };

        var candidateIds = await dbContext.PaymentRequestEventOutbox.AsNoTracking()
            .Where(x => eligibleStatuses.Contains(x.Status) && x.AvailableAtUtc <= now)
            .OrderBy(x => x.AvailableAtUtc)
            .ThenBy(x => x.EnqueuedAtUtc)
            .ThenBy(x => x.EventId)
            .Select(x => x.EventId)
            .Take(maxCount)
            .ToArrayAsync(cancellationToken);

        if (candidateIds.Length == 0)
        {
            return Array.Empty<PaymentRequestEventOutboxItem>();
        }

        var leaseToken = Guid.NewGuid();
        var leaseUntil = now.Add(leaseDuration);

        await dbContext.PaymentRequestEventOutbox
            .Where(x => candidateIds.Contains(x.EventId) && eligibleStatuses.Contains(x.Status) && x.AvailableAtUtc <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, (int)PaymentRequestEventOutboxStatus.Processing)
                .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1)
                .SetProperty(x => x.LeaseToken, leaseToken)
                .SetProperty(x => x.LeaseExpiresAtUtc, leaseUntil)
                .SetProperty(x => x.LastAttemptAtUtc, now),
                cancellationToken);

        var claimed = await dbContext.PaymentRequestEventOutbox.AsNoTracking()
            .Where(x => x.LeaseToken == leaseToken && x.Status == (int)PaymentRequestEventOutboxStatus.Processing)
            .OrderBy(x => x.EnqueuedAtUtc)
            .ThenBy(x => x.EventId)
            .ToArrayAsync(cancellationToken);

        return claimed.Select(ToItem).ToArray();
    }

    public async Task MarkDeliveredAsync(
        Guid eventId,
        DateTimeOffset deliveredAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (eventId == Guid.Empty) throw new ArgumentException("Event id cannot be empty.", nameof(eventId));
        EnsureUtc(deliveredAtUtc, nameof(deliveredAtUtc));
        var deliveredAt = ToUtcDateTime(deliveredAtUtc);

        var affected = await dbContext.PaymentRequestEventOutbox
            .Where(x => x.EventId == eventId && x.Status == (int)PaymentRequestEventOutboxStatus.Processing)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, (int)PaymentRequestEventOutboxStatus.Delivered)
                .SetProperty(x => x.DeliveredAtUtc, deliveredAt)
                .SetProperty(x => x.LeaseToken, (Guid?)null)
                .SetProperty(x => x.LeaseExpiresAtUtc, (DateTime?)null)
                .SetProperty(x => x.LastError, (string?)null),
                cancellationToken);

        if (affected != 1) throw new InvalidOperationException("Outbox event is not currently processing.");
    }

    public async Task MarkFailedAsync(
        Guid eventId,
        DateTimeOffset failedAtUtc,
        string error,
        DateTimeOffset? nextAttemptAtUtc,
        bool deadLetter,
        CancellationToken cancellationToken = default)
    {
        if (eventId == Guid.Empty) throw new ArgumentException("Event id cannot be empty.", nameof(eventId));
        EnsureUtc(failedAtUtc, nameof(failedAtUtc));
        if (string.IsNullOrWhiteSpace(error)) throw new ArgumentException("Failure error is required.", nameof(error));
        if (!deadLetter && nextAttemptAtUtc is null) throw new ArgumentException("Retry time is required for retryable failures.", nameof(nextAttemptAtUtc));
        if (nextAttemptAtUtc is not null)
        {
            EnsureUtc(nextAttemptAtUtc.Value, nameof(nextAttemptAtUtc));
            if (nextAttemptAtUtc.Value < failedAtUtc) throw new ArgumentException("Retry time cannot precede failure time.", nameof(nextAttemptAtUtc));
        }

        var failedAt = ToUtcDateTime(failedAtUtc);
        var availableAt = deadLetter ? failedAt : ToUtcDateTime(nextAttemptAtUtc!.Value);
        var status = deadLetter ? PaymentRequestEventOutboxStatus.DeadLetter : PaymentRequestEventOutboxStatus.Retry;
        var normalizedError = error.Trim();
        if (normalizedError.Length > 2048) normalizedError = normalizedError[..2048];

        var affected = await dbContext.PaymentRequestEventOutbox
            .Where(x => x.EventId == eventId && x.Status == (int)PaymentRequestEventOutboxStatus.Processing)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, (int)status)
                .SetProperty(x => x.AvailableAtUtc, availableAt)
                .SetProperty(x => x.LeaseToken, (Guid?)null)
                .SetProperty(x => x.LeaseExpiresAtUtc, (DateTime?)null)
                .SetProperty(x => x.LastAttemptAtUtc, failedAt)
                .SetProperty(x => x.LastError, normalizedError),
                cancellationToken);

        if (affected != 1) throw new InvalidOperationException("Outbox event is not currently processing.");
    }

    public async Task<int> RecoverExpiredClaimsAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        EnsureUtc(nowUtc, nameof(nowUtc));
        var now = ToUtcDateTime(nowUtc);

        return await dbContext.PaymentRequestEventOutbox
            .Where(x => x.Status == (int)PaymentRequestEventOutboxStatus.Processing &&
                        x.LeaseExpiresAtUtc != null && x.LeaseExpiresAtUtc <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, (int)PaymentRequestEventOutboxStatus.Retry)
                .SetProperty(x => x.AvailableAtUtc, now)
                .SetProperty(x => x.LeaseToken, (Guid?)null)
                .SetProperty(x => x.LeaseExpiresAtUtc, (DateTime?)null)
                .SetProperty(x => x.LastError, "Recovered after expired processing lease."),
                cancellationToken);
    }

    private static PaymentRequestEventOutboxItem ToItem(PaymentRequestEventOutboxEntity entity)
    {
        if (!Enum.IsDefined(typeof(PaymentRequestEventOutboxStatus), entity.Status))
        {
            throw new InvalidOperationException("Stored outbox status is invalid.");
        }

        return new PaymentRequestEventOutboxItem(
            new PaymentRequestEventEnvelope(
                entity.EventId,
                PaymentRequestId.From(entity.PaymentRequestId),
                entity.EventType,
                ToUtcOffset(entity.OccurredAtUtc),
                entity.PayloadJson),
            (PaymentRequestEventOutboxStatus)entity.Status,
            entity.AttemptCount,
            ToUtcOffset(entity.EnqueuedAtUtc),
            ToUtcOffset(entity.AvailableAtUtc),
            entity.LeaseExpiresAtUtc is null ? null : ToUtcOffset(entity.LeaseExpiresAtUtc.Value),
            entity.LastAttemptAtUtc is null ? null : ToUtcOffset(entity.LastAttemptAtUtc.Value),
            entity.DeliveredAtUtc is null ? null : ToUtcOffset(entity.DeliveredAtUtc.Value),
            entity.LastError);
    }

    private static void ValidateEvent(PaymentRequestEventEnvelope value)
    {
        if (value.EventId == Guid.Empty) throw new ArgumentException("Event id cannot be empty.", nameof(value));
        if (value.PaymentRequestId.Value == Guid.Empty) throw new ArgumentException("Payment request id cannot be empty.", nameof(value));
        if (string.IsNullOrWhiteSpace(value.EventType)) throw new ArgumentException("Event type is required.", nameof(value));
        if (value.EventType.Trim().Length > 128) throw new ArgumentException("Event type is too long.", nameof(value));
        if (value.PayloadJson is null) throw new ArgumentException("Payload is required.", nameof(value));
        EnsureUtc(value.OccurredAtUtc, nameof(value));
    }

    private static DateTime ToUtcDateTime(DateTimeOffset value) => value.UtcDateTime;
    private static DateTimeOffset ToUtcOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.", parameterName);
    }
}
