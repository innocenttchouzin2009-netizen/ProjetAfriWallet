using AfriWallet.PartnerWebhooks.Domain;

namespace AfriWallet.PartnerWebhooks.Application;

public sealed record SubscriptionWebhookDelivery(
    PartnerWebhookSubscriptionId SubscriptionId,
    PartnerId PartnerId,
    WebhookEndpoint Endpoint,
    WebhookSigningSecretReference SigningSecretReference,
    OutboundWebhookEvent Event);

public sealed record SubscriptionWebhookDeliveryBatchResult(
    int EligibleSubscriptionCount,
    int DispatchedDeliveryCount);
