using System.Globalization;
using System.Text.Json;
using AfriWallet.PaymentRequests.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.DeliverySubscriptions.Persistence;

public sealed class EfPaymentRequestDeliverySubscriptionRegistry(
    PaymentRequestDeliverySubscriptionDbContext db) : IPaymentRequestDeliverySubscriptionRegistry
{
    public async Task<PaymentRequestDeliverySubscription?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Subscription id cannot be empty.", nameof(id));
        var entity = await db.Subscriptions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<PaymentRequestDeliverySubscription>> ListActiveAsync(
        PaymentRequestDeliveryRecipientBinding recipient, string eventType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recipient);
        var normalizedEvent = PaymentRequestDeliverySubscription.NormalizeEventType(eventType);
        var entities = await db.Subscriptions.AsNoTracking()
            .Where(x => x.Status == (int)PaymentRequestDeliverySubscriptionStatus.Active &&
                        x.RecipientKind == recipient.Kind && x.RecipientKey == recipient.Key)
            .OrderBy(x => x.Id).ToListAsync(cancellationToken);
        return entities.Select(Map).Where(x => x.Authorizes(normalizedEvent)).ToArray();
    }

    public async Task AddAsync(PaymentRequestDeliverySubscription subscription, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        db.Subscriptions.Add(Map(subscription));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PaymentRequestDeliverySubscription subscription, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        var entity = await db.Subscriptions.SingleOrDefaultAsync(x => x.Id == subscription.Id, cancellationToken)
            ?? throw new InvalidOperationException("Delivery subscription was not found.");
        entity.Status = (int)subscription.Status;
        entity.UpdatedAtUtc = Format(subscription.UpdatedAtUtc);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static PaymentRequestDeliverySubscriptionEntity Map(PaymentRequestDeliverySubscription x) => new()
    {
        Id = x.Id, OwnerId = x.OwnerId, RecipientKind = x.Recipient.Kind, RecipientKey = x.Recipient.Key,
        EndpointUrl = x.Endpoint.AbsoluteUri, Status = (int)x.Status, KeyId = x.KeyId,
        SecretReference = x.SecretReference, EventTypesJson = JsonSerializer.Serialize(x.EventTypes),
        MaxAttempts = x.RetryPolicy.MaxAttempts,
        BaseRetryDelaySeconds = checked((long)x.RetryPolicy.BaseRetryDelay.TotalSeconds),
        CreatedAtUtc = Format(x.CreatedAtUtc), UpdatedAtUtc = Format(x.UpdatedAtUtc)
    };

    private static PaymentRequestDeliverySubscription Map(PaymentRequestDeliverySubscriptionEntity x) =>
        PaymentRequestDeliverySubscription.Restore(
            x.Id, x.OwnerId, new PaymentRequestDeliveryRecipientBinding(x.RecipientKind, x.RecipientKey),
            new Uri(x.EndpointUrl, UriKind.Absolute),
            (PaymentRequestDeliverySubscriptionStatus)x.Status, x.KeyId, x.SecretReference,
            JsonSerializer.Deserialize<string[]>(x.EventTypesJson) ?? Array.Empty<string>(),
            new PaymentRequestDeliveryRetryPolicy(x.MaxAttempts, TimeSpan.FromSeconds(x.BaseRetryDelaySeconds)),
            Parse(x.CreatedAtUtc), Parse(x.UpdatedAtUtc));

    private static string Format(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);
    private static DateTimeOffset Parse(string value) =>
        DateTimeOffset.ParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
