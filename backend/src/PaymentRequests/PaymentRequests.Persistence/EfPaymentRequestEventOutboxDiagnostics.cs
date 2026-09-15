using AfriWallet.PaymentRequests.Application;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.Persistence;

public sealed class EfPaymentRequestEventOutboxDiagnostics(PaymentRequestDbContext dbContext)
    : IPaymentRequestEventOutboxDiagnostics
{
    public async Task<PaymentRequestEventOutboxDiagnosticsSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = dbContext.PaymentRequestEventOutbox.AsNoTracking();
        var pending = await rows.LongCountAsync(x => x.Status == (int)PaymentRequestEventOutboxStatus.Pending, cancellationToken);
        var processing = await rows.LongCountAsync(x => x.Status == (int)PaymentRequestEventOutboxStatus.Processing, cancellationToken);
        var retry = await rows.LongCountAsync(x => x.Status == (int)PaymentRequestEventOutboxStatus.Retry, cancellationToken);
        var delivered = await rows.LongCountAsync(x => x.Status == (int)PaymentRequestEventOutboxStatus.Delivered, cancellationToken);
        var deadLetter = await rows.LongCountAsync(x => x.Status == (int)PaymentRequestEventOutboxStatus.DeadLetter, cancellationToken);

        var oldestUndelivered = await rows
            .Where(x => x.Status != (int)PaymentRequestEventOutboxStatus.Delivered &&
                        x.Status != (int)PaymentRequestEventOutboxStatus.DeadLetter)
            .OrderBy(x => x.EnqueuedAtUtc)
            .Select(x => (DateTime?)x.EnqueuedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var latestDeadLetter = await rows
            .Where(x => x.Status == (int)PaymentRequestEventOutboxStatus.DeadLetter)
            .OrderByDescending(x => x.LastAttemptAtUtc ?? x.EnqueuedAtUtc)
            .Select(x => (DateTime?)(x.LastAttemptAtUtc ?? x.EnqueuedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        return new PaymentRequestEventOutboxDiagnosticsSnapshot(
            pending,
            processing,
            retry,
            delivered,
            deadLetter,
            oldestUndelivered is null ? null : ToUtcOffset(oldestUndelivered.Value),
            latestDeadLetter is null ? null : ToUtcOffset(latestDeadLetter.Value));
    }

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
