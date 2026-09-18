using AfriWallet.Webhooks.Domain;

namespace AfriWallet.Webhooks.Application;

public interface IWebhookSubscriptionRepository
{
    Task<WebhookSubscription?> GetAsync(
        WebhookSubscriptionId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WebhookSubscription>> ListByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        WebhookSubscription subscription,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        WebhookSubscription subscription,
        CancellationToken cancellationToken = default);
}
