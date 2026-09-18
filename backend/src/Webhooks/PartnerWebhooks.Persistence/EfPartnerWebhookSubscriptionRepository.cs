using AfriWallet.PartnerWebhooks.Application;
using AfriWallet.PartnerWebhooks.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PartnerWebhooks.Persistence;

public sealed class EfPartnerWebhookSubscriptionRepository(PartnerWebhookDbContext dbContext)
    : IPartnerWebhookSubscriptionRepository
{
    public async Task<PartnerWebhookSubscription?> GetAsync(
        PartnerWebhookSubscriptionId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await dbContext.Subscriptions
            .AsNoTracking()
            .Include(x => x.EventTypes)
            .SingleOrDefaultAsync(x => x.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task AddAsync(
        PartnerWebhookSubscription subscription,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        cancellationToken.ThrowIfCancellationRequested();

        await dbContext.Subscriptions.AddAsync(ToEntity(subscription), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        PartnerWebhookSubscription subscription,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await dbContext.Subscriptions
            .Include(x => x.EventTypes)
            .SingleOrDefaultAsync(x => x.Id == subscription.Id.Value, cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException("Webhook subscription was not found.");
        }

        entity.PartnerId = subscription.PartnerId.Value;
        entity.Endpoint = subscription.Endpoint.Value;
        entity.SigningSecretReference = subscription.SigningSecretReference.Value;
        entity.Status = (int)subscription.Status;
        entity.CreatedAtUtc = FormatUtc(subscription.CreatedAtUtc);
        entity.UpdatedAtUtc = FormatUtc(subscription.UpdatedAtUtc);

        entity.EventTypes.Clear();
        foreach (var eventType in subscription.EventTypes)
        {
            entity.EventTypes.Add(new PartnerWebhookSubscriptionEventTypeEntity
            {
                SubscriptionId = subscription.Id.Value,
                EventType = eventType.Value
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PartnerWebhookSubscription>> ListActiveByEventTypeAsync(
        WebhookEventType eventType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entities = await dbContext.Subscriptions
            .AsNoTracking()
            .Include(x => x.EventTypes)
            .Where(x =>
                x.Status == (int)PartnerWebhookSubscriptionStatus.Active &&
                x.EventTypes.Any(e => e.EventType == eventType.Value))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToArray();
    }

    private static PartnerWebhookSubscriptionEntity ToEntity(PartnerWebhookSubscription subscription) =>
        new()
        {
            Id = subscription.Id.Value,
            PartnerId = subscription.PartnerId.Value,
            Endpoint = subscription.Endpoint.Value,
            SigningSecretReference = subscription.SigningSecretReference.Value,
            Status = (int)subscription.Status,
            CreatedAtUtc = FormatUtc(subscription.CreatedAtUtc),
            UpdatedAtUtc = FormatUtc(subscription.UpdatedAtUtc),
            EventTypes = subscription.EventTypes.Select(eventType =>
                new PartnerWebhookSubscriptionEventTypeEntity
                {
                    SubscriptionId = subscription.Id.Value,
                    EventType = eventType.Value
                }).ToList()
        };

    private static PartnerWebhookSubscription ToDomain(PartnerWebhookSubscriptionEntity entity) =>
        PartnerWebhookSubscription.Restore(
            PartnerWebhookSubscriptionId.From(entity.Id),
            PartnerId.From(entity.PartnerId),
            WebhookEndpoint.From(entity.Endpoint),
            WebhookSigningSecretReference.From(entity.SigningSecretReference),
            entity.EventTypes.Select(item => WebhookEventType.From(item.EventType)),
            ParseStatus(entity.Status),
            ParseUtc(entity.CreatedAtUtc),
            ParseUtc(entity.UpdatedAtUtc));

    private static PartnerWebhookSubscriptionStatus ParseStatus(int value) =>
        Enum.IsDefined(typeof(PartnerWebhookSubscriptionStatus), value)
            ? (PartnerWebhookSubscriptionStatus)value
            : throw new InvalidOperationException($"Unknown webhook subscription status '{value}'.");

    private static string FormatUtc(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseUtc(string value)
    {
        if (!DateTimeOffset.TryParseExact(
                value,
                "O",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var parsed))
        {
            throw new InvalidOperationException("Stored webhook subscription timestamp is invalid.");
        }

        return parsed.ToUniversalTime();
    }
}
