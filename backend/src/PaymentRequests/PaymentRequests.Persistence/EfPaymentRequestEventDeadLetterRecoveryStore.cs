using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.Persistence;

public sealed class EfPaymentRequestEventDeadLetterRecoveryStore(PaymentRequestDbContext dbContext)
    : IPaymentRequestEventDeadLetterRecoveryStore
{
    public async Task<PaymentRequestEventOutboxItem?> GetAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event id cannot be empty.", nameof(eventId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var entity = await dbContext.PaymentRequestEventOutbox
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.EventId == eventId, cancellationToken);

        return entity is null ? null : ToItem(entity);
    }

    public async Task<bool> TryRequeueAsync(
        PaymentRequestEventDeadLetterReplayPlan plan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.EventId == Guid.Empty)
        {
            throw new ArgumentException("Event id cannot be empty.", nameof(plan));
        }

        if (plan.ExpectedAttemptCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(plan), "Expected attempt count must be positive.");
        }

        if (plan.ReplayOrdinal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(plan), "Replay ordinal must be positive.");
        }

        EnsureUtc(plan.AvailableAtUtc, nameof(plan.AvailableAtUtc));
        _ = NormalizeRequired(plan.RequestedBy, 128, nameof(plan.RequestedBy));
        _ = NormalizeRequired(plan.Reason, 512, nameof(plan.Reason));
        cancellationToken.ThrowIfCancellationRequested();

        var availableAtUtc = plan.AvailableAtUtc.UtcDateTime;

        var affected = await dbContext.PaymentRequestEventOutbox
            .Where(x =>
                x.EventId == plan.EventId &&
                x.Status == (int)PaymentRequestEventOutboxStatus.DeadLetter &&
                x.AttemptCount == plan.ExpectedAttemptCount)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, (int)PaymentRequestEventOutboxStatus.Retry)
                .SetProperty(x => x.AvailableAtUtc, availableAtUtc)
                .SetProperty(x => x.LeaseToken, (Guid?)null)
                .SetProperty(x => x.LeaseExpiresAtUtc, (DateTime?)null),
                cancellationToken);

        return affected == 1;
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

    private static string NormalizeRequired(string value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);
        }

        return normalized;
    }

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }
    }
}
