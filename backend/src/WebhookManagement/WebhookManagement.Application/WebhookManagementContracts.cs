using AfriWallet.Webhooks.Domain;

namespace AfriWallet.Webhooks.Application;

public sealed record RegisterWebhookSubscriptionCommand(
    Guid OwnerId,
    Uri Endpoint,
    IReadOnlyCollection<string> EventTypes,
    string SigningKeyReference,
    DateTimeOffset CreatedAtUtc);

public sealed record WebhookSubscriptionSnapshot(
    Guid Id,
    Guid OwnerId,
    string Endpoint,
    IReadOnlyList<string> EventTypes,
    string SigningKeyReference,
    WebhookSubscriptionStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

internal static class WebhookSubscriptionMappings
{
    public static WebhookSubscriptionSnapshot ToSnapshot(WebhookSubscription subscription) =>
        new(
            subscription.Id.Value,
            subscription.OwnerId,
            subscription.Endpoint.AbsoluteUri,
            subscription.EventTypes.Select(eventType => eventType.Value).ToArray(),
            subscription.SigningKeyReference,
            subscription.Status,
            subscription.CreatedAtUtc,
            subscription.UpdatedAtUtc);
}
