using AfriWallet.Webhooks.Application;
using AfriWallet.Webhooks.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Webhooks.Persistence;

public sealed class EfWebhookSubscriptionRepository(WebhookManagementDbContext dbContext)
    : IWebhookSubscriptionRepository
{
    public async Task<WebhookSubscription?> GetAsync(
        WebhookSubscriptionId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entity = await dbContext.WebhookSubscriptions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id.Value, cancellationToken);
        return entity is null ? null : WebhookSubscriptionEntityMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<WebhookSubscription>> ListByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        if (ownerId == Guid.Empty)
            throw new ArgumentException("Webhook owner id cannot be empty.", nameof(ownerId));

        cancellationToken.ThrowIfCancellationRequested();
        var entities = await dbContext.WebhookSubscriptions
            .AsNoTracking()
            .Where(x => x.OwnerId == ownerId)
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(WebhookSubscriptionEntityMapper.ToDomain).ToArray();
    }

    public async Task AddAsync(
        WebhookSubscription subscription,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        cancellationToken.ThrowIfCancellationRequested();

        dbContext.WebhookSubscriptions.Add(WebhookSubscriptionEntityMapper.ToEntity(subscription));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            dbContext.ChangeTracker.Clear();
            throw new InvalidOperationException("Webhook subscription id is already persisted.", exception);
        }
    }

    public async Task UpdateAsync(
        WebhookSubscription subscription,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await dbContext.WebhookSubscriptions
            .SingleOrDefaultAsync(x => x.Id == subscription.Id.Value, cancellationToken);

        if (entity is null)
            throw new InvalidOperationException("Webhook subscription was not found.");

        WebhookSubscriptionEntityMapper.Apply(entity, subscription);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
