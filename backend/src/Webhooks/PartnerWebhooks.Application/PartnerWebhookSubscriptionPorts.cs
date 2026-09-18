using AfriWallet.PartnerWebhooks.Domain;

namespace AfriWallet.PartnerWebhooks.Application;

public interface IPartnerWebhookSubscriptionRepository
{
    Task<PartnerWebhookSubscription?> GetAsync(
        PartnerWebhookSubscriptionId id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        PartnerWebhookSubscription subscription,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        PartnerWebhookSubscription subscription,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PartnerWebhookSubscription>> ListActiveByEventTypeAsync(
        WebhookEventType eventType,
        CancellationToken cancellationToken = default);
}
