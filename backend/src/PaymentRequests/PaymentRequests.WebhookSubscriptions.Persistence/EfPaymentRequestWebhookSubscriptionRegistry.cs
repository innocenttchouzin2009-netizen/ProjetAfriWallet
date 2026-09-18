using System.Globalization;
using System.Text.Json;
using AfriWallet.PaymentRequests.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.WebhookSubscriptions.Persistence;

public sealed class EfPaymentRequestWebhookSubscriptionRegistry(
    PaymentRequestWebhookSubscriptionDbContext dbContext)
    : IPaymentRequestWebhookSubscriptionRegistry
{
    public async Task<PaymentRequestWebhookSubscription?> GetAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        if (subscriptionId == Guid.Empty) throw new ArgumentException("Subscription id cannot be empty.", nameof(subscriptionId));
        var entity = await dbContext.Subscriptions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<PaymentRequestWebhookSubscription>> ListActiveForEventAsync(
        string eventType,
        CancellationToken cancellationToken = default)
    {
        var normalized = PaymentRequestWebhookSubscription.NormalizeEventType(eventType);
        var entities = await dbContext.Subscriptions.AsNoTracking()
            .Where(x => x.Status == (int)PaymentRequestWebhookSubscriptionStatus.Active)
            .OrderBy(x => x.IntegrationId)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(Map)
            .Where(x => x.SubscribesTo(normalized))
            .ToArray();
    }

    public async Task<IReadOnlyList<PaymentRequestWebhookSubscription>> ListForIntegrationAsync(
        string integrationId,
        CancellationToken cancellationToken = default)
    {
        var normalized = PaymentRequestWebhookSubscription.NormalizeIntegrationId(integrationId);
        var entities = await dbContext.Subscriptions.AsNoTracking()
            .Where(x => x.IntegrationId == normalized)
            .OrderBy(x => x.MerchantId)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(Map).ToArray();
    }

    public async Task AddAsync(
        PaymentRequestWebhookSubscription subscription,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        dbContext.Subscriptions.Add(Map(subscription));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        PaymentRequestWebhookSubscription subscription,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        var entity = await dbContext.Subscriptions.SingleOrDefaultAsync(x => x.Id == subscription.Id, cancellationToken)
            ?? throw new InvalidOperationException("Webhook subscription was not found.");

        entity.IntegrationId = subscription.IntegrationId;
        entity.MerchantId = subscription.MerchantId;
        entity.EndpointUrl = subscription.Endpoint.AbsoluteUri;
        entity.Status = (int)subscription.Status;
        entity.KeyId = subscription.KeyId;
        entity.SecretReference = subscription.SecretReference;
        entity.EventTypesJson = JsonSerializer.Serialize(subscription.EventTypes);
        entity.UpdatedAtUtc = Format(subscription.UpdatedAtUtc);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static PaymentRequestWebhookSubscriptionEntity Map(PaymentRequestWebhookSubscription value) => new()
    {
        Id = value.Id,
        IntegrationId = value.IntegrationId,
        MerchantId = value.MerchantId,
        EndpointUrl = value.Endpoint.AbsoluteUri,
        Status = (int)value.Status,
        KeyId = value.KeyId,
        SecretReference = value.SecretReference,
        EventTypesJson = JsonSerializer.Serialize(value.EventTypes),
        CreatedAtUtc = Format(value.CreatedAtUtc),
        UpdatedAtUtc = Format(value.UpdatedAtUtc)
    };

    private static PaymentRequestWebhookSubscription Map(PaymentRequestWebhookSubscriptionEntity value) =>
        PaymentRequestWebhookSubscription.Restore(
            value.Id,
            value.IntegrationId,
            value.MerchantId,
            new Uri(value.EndpointUrl, UriKind.Absolute),
            (PaymentRequestWebhookSubscriptionStatus)value.Status,
            value.KeyId,
            value.SecretReference,
            JsonSerializer.Deserialize<string[]>(value.EventTypesJson) ?? Array.Empty<string>(),
            Parse(value.CreatedAtUtc),
            Parse(value.UpdatedAtUtc));

    private static string Format(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);
    private static DateTimeOffset Parse(string value) =>
        DateTimeOffset.ParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
