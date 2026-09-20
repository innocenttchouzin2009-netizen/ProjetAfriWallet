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

        var rows = await dbContext.PaymentRequestEventOutbox
            .AsNoTracking()
            .Select(x => new DiagnosticRow(x.Status, x.EnqueuedAtUtc, x.LastAttemptAtUtc))
            .ToListAsync(cancellationToken);

        long Count(PaymentRequestEventOutboxStatus status) =>
            rows.LongCount(x => x.Status == (int)status);

        var oldestUndelivered = rows
            .Where(x => x.Status is (int)PaymentRequestEventOutboxStatus.Pending or
                (int)PaymentRequestEventOutboxStatus.Processing or
                (int)PaymentRequestEventOutboxStatus.Retry)
            .Select(x => (DateTime?)x.EnqueuedAtUtc)
            .Min();

        var latestDeadLetter = rows
            .Where(x => x.Status == (int)PaymentRequestEventOutboxStatus.DeadLetter)
            .Select(x => x.LastAttemptAtUtc)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .DefaultIfEmpty()
            .Max();

        return new PaymentRequestEventOutboxDiagnosticsSnapshot(
            Count(PaymentRequestEventOutboxStatus.Pending),
            Count(PaymentRequestEventOutboxStatus.Processing),
            Count(PaymentRequestEventOutboxStatus.Retry),
            Count(PaymentRequestEventOutboxStatus.Delivered),
            Count(PaymentRequestEventOutboxStatus.DeadLetter),
            ToUtc(oldestUndelivered),
            latestDeadLetter == default ? null : ToUtc(latestDeadLetter));
    }

    private static DateTimeOffset? ToUtc(DateTime? value) =>
        value is null ? null : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));

    private static DateTimeOffset ToUtc(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private sealed record DiagnosticRow(int Status, DateTime EnqueuedAtUtc, DateTime? LastAttemptAtUtc);
}
