using AfriWallet.PartnerWebhooks.Domain;

namespace AfriWallet.PartnerWebhooks.Application;

public sealed record CreatePartnerWebhookSubscriptionCommand(
    PartnerId PartnerId,
    WebhookEndpoint Endpoint,
    WebhookSigningSecretReference SigningSecretReference,
    IReadOnlyCollection<WebhookEventType> EventTypes,
    DateTimeOffset CreatedAtUtc);

public sealed record UpdatePartnerWebhookSubscriptionCommand(
    PartnerWebhookSubscriptionId Id,
    PartnerId PartnerId,
    WebhookEndpoint? Endpoint,
    WebhookSigningSecretReference? SigningSecretReference,
    IReadOnlyCollection<WebhookEventType>? EventTypes,
    DateTimeOffset UpdatedAtUtc);

public sealed record PartnerWebhookSubscriptionSnapshot(
    PartnerWebhookSubscriptionId Id,
    PartnerId PartnerId,
    WebhookEndpoint Endpoint,
    WebhookSigningSecretReference SigningSecretReference,
    IReadOnlyList<WebhookEventType> EventTypes,
    PartnerWebhookSubscriptionStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

internal static class PartnerWebhookSubscriptionMappings
{
    public static PartnerWebhookSubscriptionSnapshot ToSnapshot(PartnerWebhookSubscription subscription) => new(
        subscription.Id,
        subscription.PartnerId,
        subscription.Endpoint,
        subscription.SigningSecretReference,
        subscription.EventTypes.ToArray(),
        subscription.Status,
        subscription.CreatedAtUtc,
        subscription.UpdatedAtUtc);
}
