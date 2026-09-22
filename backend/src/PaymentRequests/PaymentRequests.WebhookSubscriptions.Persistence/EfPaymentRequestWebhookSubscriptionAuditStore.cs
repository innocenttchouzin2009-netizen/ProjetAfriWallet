using System.Globalization;
using AfriWallet.PaymentRequests.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.WebhookSubscriptions.Persistence;

public sealed class EfPaymentRequestWebhookSubscriptionAuditStore(
    PaymentRequestWebhookSubscriptionDbContext dbContext)
    : IPaymentRequestWebhookSubscriptionAuditStore
{
    public async Task AppendAsync(
        PaymentRequestWebhookSubscriptionAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        dbContext.AuditEntries.Add(new PaymentRequestWebhookSubscriptionAuditEntity
        {
            Id = entry.Id,
            SubscriptionId = entry.SubscriptionId,
            IntegrationId = entry.IntegrationId,
            MerchantId = entry.MerchantId,
            ActorSubject = entry.ActorSubject,
            Operation = (int)entry.Operation,
            KeyId = entry.KeyId,
            SecretReference = entry.SecretReference,
            Succeeded = entry.Succeeded,
            HttpStatusCode = entry.HttpStatusCode,
            Detail = entry.Detail,
            OccurredAtUtc = entry.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture)
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentRequestWebhookSubscriptionAuditEntry>> ListAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription id cannot be empty.", nameof(subscriptionId));

        var rows = await dbContext.AuditEntries.AsNoTracking()
            .Where(x => x.SubscriptionId == subscriptionId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        return rows.Select(x => new PaymentRequestWebhookSubscriptionAuditEntry(
            x.Id,
            x.SubscriptionId,
            x.IntegrationId,
            x.MerchantId,
            x.ActorSubject,
            (PaymentRequestWebhookSubscriptionAuditOperation)x.Operation,
            x.KeyId,
            x.SecretReference,
            x.Succeeded,
            x.HttpStatusCode,
            x.Detail,
            DateTimeOffset.ParseExact(x.OccurredAtUtc, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)))
            .ToArray();
    }
}
