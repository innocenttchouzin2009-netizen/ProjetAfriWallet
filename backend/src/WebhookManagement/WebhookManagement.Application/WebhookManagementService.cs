using AfriWallet.Webhooks.Domain;

namespace AfriWallet.Webhooks.Application;

public sealed class WebhookManagementService(IWebhookSubscriptionRepository repository)
{
    public async Task<WebhookSubscriptionSnapshot> RegisterAsync(
        RegisterWebhookSubscriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.OwnerId == Guid.Empty)
            throw new ArgumentException("Webhook owner id cannot be empty.", nameof(command));
        ArgumentNullException.ThrowIfNull(command.Endpoint);
        ArgumentNullException.ThrowIfNull(command.EventTypes);

        var eventTypes = command.EventTypes.Select(WebhookEventType.Create).ToArray();
        var subscription = WebhookSubscription.Create(
            command.OwnerId,
            command.Endpoint,
            eventTypes,
            command.SigningKeyReference,
            command.CreatedAtUtc);

        await repository.AddAsync(subscription, cancellationToken);
        return WebhookSubscriptionMappings.ToSnapshot(subscription);
    }

    public async Task<WebhookSubscriptionSnapshot?> GetOwnedAsync(
        Guid ownerId,
        WebhookSubscriptionId id,
        CancellationToken cancellationToken = default)
    {
        EnsureOwner(ownerId);
        cancellationToken.ThrowIfCancellationRequested();

        var subscription = await repository.GetAsync(id, cancellationToken);
        return subscription is null || subscription.OwnerId != ownerId
            ? null
            : WebhookSubscriptionMappings.ToSnapshot(subscription);
    }

    public async Task<IReadOnlyList<WebhookSubscriptionSnapshot>> ListAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        EnsureOwner(ownerId);
        cancellationToken.ThrowIfCancellationRequested();

        var subscriptions = await repository.ListByOwnerAsync(ownerId, cancellationToken);
        return subscriptions
            .OrderBy(subscription => subscription.CreatedAtUtc)
            .ThenBy(subscription => subscription.Id.Value)
            .Select(WebhookSubscriptionMappings.ToSnapshot)
            .ToArray();
    }

    public Task<WebhookSubscriptionSnapshot?> DisableAsync(
        Guid ownerId,
        WebhookSubscriptionId id,
        DateTimeOffset changedAtUtc,
        CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(ownerId, id, changedAtUtc, enabled: false, cancellationToken);

    public Task<WebhookSubscriptionSnapshot?> EnableAsync(
        Guid ownerId,
        WebhookSubscriptionId id,
        DateTimeOffset changedAtUtc,
        CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(ownerId, id, changedAtUtc, enabled: true, cancellationToken);

    private async Task<WebhookSubscriptionSnapshot?> ChangeStatusAsync(
        Guid ownerId,
        WebhookSubscriptionId id,
        DateTimeOffset changedAtUtc,
        bool enabled,
        CancellationToken cancellationToken)
    {
        EnsureOwner(ownerId);
        cancellationToken.ThrowIfCancellationRequested();

        var subscription = await repository.GetAsync(id, cancellationToken);
        if (subscription is null || subscription.OwnerId != ownerId)
            return null;

        if (enabled)
            subscription.Enable(changedAtUtc);
        else
            subscription.Disable(changedAtUtc);

        await repository.UpdateAsync(subscription, cancellationToken);
        return WebhookSubscriptionMappings.ToSnapshot(subscription);
    }

    private static void EnsureOwner(Guid ownerId)
    {
        if (ownerId == Guid.Empty)
            throw new ArgumentException("Webhook owner id cannot be empty.", nameof(ownerId));
    }
}
