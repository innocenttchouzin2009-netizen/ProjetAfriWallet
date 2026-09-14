using AfriWallet.PaymentRequests.Application;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.Persistence;

public sealed class EfPaymentRequestOutboxStore(PaymentRequestDbContext dbContext)
    : IPaymentRequestOutboxStore
{
    public async Task<IReadOnlyList<PaymentRequestOutboxDeliveryMessage>> ReadPendingAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        if (maxCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCount), "Max count must be positive.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var rows = await dbContext.PaymentRequestIntegrationOutbox
            .AsNoTracking()
            .Where(x => x.PublishedAtUtc == null)
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.Id)
            .Take(maxCount)
            .ToArrayAsync(cancellationToken);

        return rows.Select(ToDeliveryMessage).ToArray();
    }

    public async Task MarkPublishedAsync(
        Guid messageId,
        DateTimeOffset publishedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("Outbox message id cannot be empty.", nameof(messageId));
        }

        if (publishedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Published timestamp must be UTC.", nameof(publishedAtUtc));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var entity = await dbContext.PaymentRequestIntegrationOutbox
            .SingleOrDefaultAsync(x => x.Id == messageId, cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException("Outbox message was not found.");
        }

        if (entity.PublishedAtUtc is not null)
        {
            return;
        }

        entity.PublishedAtUtc = publishedAtUtc.ToString("O");
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static PaymentRequestOutboxDeliveryMessage ToDeliveryMessage(PaymentRequestOutboxMessage row)
    {
        if (!DateTimeOffset.TryParse(row.OccurredAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var occurredAtUtc) ||
            occurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException("Outbox message contains an invalid UTC occurrence timestamp.");
        }

        return new PaymentRequestOutboxDeliveryMessage(
            row.Id,
            row.PaymentRequestId,
            row.EventType,
            row.PayloadJson,
            occurredAtUtc);
    }
}
