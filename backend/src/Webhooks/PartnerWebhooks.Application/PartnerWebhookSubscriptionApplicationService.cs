using AfriWallet.PartnerWebhooks.Domain;

namespace AfriWallet.PartnerWebhooks.Application;

public sealed class PartnerWebhookSubscriptionApplicationService(
    IPartnerWebhookSubscriptionRepository repository)
{
    public async Task<PartnerWebhookSubscriptionSnapshot> CreateAsync(
        CreatePartnerWebhookSubscriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var subscription = PartnerWebhookSubscription.Create(
            command.PartnerId,
            command.Endpoint,
            command.SigningSecretReference,
            command.EventTypes,
            command.CreatedAtUtc);

        await repository.AddAsync(subscription, cancellationToken);
        return PartnerWebhookSubscriptionMappings.ToSnapshot(subscription);
    }

    public async Task<PartnerWebhookSubscriptionSnapshot?> GetAsync(
        PartnerWebhookSubscriptionId id,
        PartnerId partnerId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var subscription = await repository.GetAsync(id, cancellationToken);
        return subscription is null || subscription.PartnerId != partnerId
            ? null
            : PartnerWebhookSubscriptionMappings.ToSnapshot(subscription);
    }

    public async Task<PartnerWebhookSubscriptionSnapshot?> UpdateAsync(
        UpdatePartnerWebhookSubscriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.Endpoint is null &&
            command.SigningSecretReference is null &&
            command.EventTypes is null)
        {
            throw new ArgumentException("At least one webhook subscription field must be updated.", nameof(command));
        }

        var subscription = await GetOwnedAsync(command.Id, command.PartnerId, cancellationToken);
        if (subscription is null)
        {
            return null;
        }

        if (command.Endpoint is { } endpoint)
        {
            subscription.UpdateEndpoint(endpoint, command.UpdatedAtUtc);
        }

        if (command.SigningSecretReference is { } secretReference)
        {
            subscription.RotateSigningSecretReference(secretReference, command.UpdatedAtUtc);
        }

        if (command.EventTypes is { } eventTypes)
        {
            subscription.ReplaceEventTypes(eventTypes, command.UpdatedAtUtc);
        }

        await repository.UpdateAsync(subscription, cancellationToken);
        return PartnerWebhookSubscriptionMappings.ToSnapshot(subscription);
    }

    public Task<PartnerWebhookSubscriptionSnapshot?> SuspendAsync(
        PartnerWebhookSubscriptionId id,
        PartnerId partnerId,
        DateTimeOffset suspendedAtUtc,
        CancellationToken cancellationToken = default) =>
        MutateAsync(id, partnerId, subscription => subscription.Suspend(suspendedAtUtc), cancellationToken);

    public Task<PartnerWebhookSubscriptionSnapshot?> ResumeAsync(
        PartnerWebhookSubscriptionId id,
        PartnerId partnerId,
        DateTimeOffset resumedAtUtc,
        CancellationToken cancellationToken = default) =>
        MutateAsync(id, partnerId, subscription => subscription.Resume(resumedAtUtc), cancellationToken);

    public Task<PartnerWebhookSubscriptionSnapshot?> RevokeAsync(
        PartnerWebhookSubscriptionId id,
        PartnerId partnerId,
        DateTimeOffset revokedAtUtc,
        CancellationToken cancellationToken = default) =>
        MutateAsync(id, partnerId, subscription => subscription.Revoke(revokedAtUtc), cancellationToken);

    public async Task<IReadOnlyList<PartnerWebhookSubscriptionSnapshot>> ListActiveByEventTypeAsync(
        WebhookEventType eventType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var subscriptions = await repository.ListActiveByEventTypeAsync(eventType, cancellationToken);

        return subscriptions
            .Where(subscription => subscription.Accepts(eventType))
            .Select(PartnerWebhookSubscriptionMappings.ToSnapshot)
            .ToArray();
    }

    private async Task<PartnerWebhookSubscriptionSnapshot?> MutateAsync(
        PartnerWebhookSubscriptionId id,
        PartnerId partnerId,
        Action<PartnerWebhookSubscription> mutation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var subscription = await GetOwnedAsync(id, partnerId, cancellationToken);
        if (subscription is null)
        {
            return null;
        }

        mutation(subscription);
        await repository.UpdateAsync(subscription, cancellationToken);
        return PartnerWebhookSubscriptionMappings.ToSnapshot(subscription);
    }

    private async Task<PartnerWebhookSubscription?> GetOwnedAsync(
        PartnerWebhookSubscriptionId id,
        PartnerId partnerId,
        CancellationToken cancellationToken)
    {
        var subscription = await repository.GetAsync(id, cancellationToken);
        return subscription is null || subscription.PartnerId != partnerId
            ? null
            : subscription;
    }
}
