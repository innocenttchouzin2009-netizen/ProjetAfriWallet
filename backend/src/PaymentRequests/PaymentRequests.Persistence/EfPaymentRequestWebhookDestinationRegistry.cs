using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.Persistence;

public sealed class EfPaymentRequestWebhookDestinationRegistry(
    PaymentRequestWebhookRegistryDbContext dbContext)
    : IPaymentRequestWebhookDestinationRegistry
{
    public async Task<PaymentRequestWebhookDestination?> GetAsync(
        PaymentRequestWebhookDestinationId destinationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await dbContext.WebhookDestinations
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == destinationId.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<PaymentRequestWebhookDestination>> ListByRecipientAsync(
        PaymentRequestWebhookRecipientId recipientId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entities = await dbContext.WebhookDestinations
            .AsNoTracking()
            .Where(x => x.RecipientId == recipientId.Value)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToArray();
    }

    public async Task<IReadOnlyList<PaymentRequestWebhookDestination>> ResolveActiveForEventAsync(
        PaymentRequestWebhookRecipientId recipientId,
        PaymentRequestLifecycleEventKind eventKind,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Enum.IsDefined(eventKind))
            throw new ArgumentOutOfRangeException(nameof(eventKind));

        var entities = await dbContext.WebhookDestinations
            .AsNoTracking()
            .Where(x => x.RecipientId == recipientId.Value && x.IsActive)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return entities
            .Select(ToDomain)
            .Where(destination => destination.IsSubscribedTo(eventKind))
            .ToArray();
    }

    public async Task AddAsync(
        PaymentRequestWebhookDestination destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        cancellationToken.ThrowIfCancellationRequested();

        dbContext.WebhookDestinations.Add(ToEntity(destination));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        PaymentRequestWebhookDestination destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await dbContext.WebhookDestinations
            .SingleOrDefaultAsync(x => x.Id == destination.Id.Value, cancellationToken);

        if (entity is null)
            throw new InvalidOperationException("Webhook destination was not found.");

        Copy(destination, entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static PaymentRequestWebhookDestination ToDomain(
        PaymentRequestWebhookDestinationEntity entity) =>
        PaymentRequestWebhookDestination.Restore(
            PaymentRequestWebhookDestinationId.From(entity.Id),
            PaymentRequestWebhookRecipientId.From(entity.RecipientId),
            new Uri(entity.Endpoint, UriKind.Absolute),
            entity.IsActive,
            ParseEvents(entity.SubscribedEvents),
            new PaymentRequestWebhookRetryPolicy(
                entity.RetryMaxAttempts,
                TimeSpan.FromMilliseconds(entity.RetryBaseDelayMilliseconds),
                TimeSpan.FromMilliseconds(entity.RetryMaxDelayMilliseconds)));

    private static PaymentRequestWebhookDestinationEntity ToEntity(
        PaymentRequestWebhookDestination destination)
    {
        var entity = new PaymentRequestWebhookDestinationEntity();
        Copy(destination, entity);
        return entity;
    }

    private static void Copy(
        PaymentRequestWebhookDestination destination,
        PaymentRequestWebhookDestinationEntity entity)
    {
        entity.Id = destination.Id.Value;
        entity.RecipientId = destination.RecipientId.Value;
        entity.Endpoint = destination.Endpoint.AbsoluteUri;
        entity.IsActive = destination.IsActive;
        entity.SubscribedEvents = SerializeEvents(destination.SubscribedEvents);
        entity.RetryMaxAttempts = destination.RetryPolicy.MaxAttempts;
        entity.RetryBaseDelayMilliseconds = checked((long)destination.RetryPolicy.BaseDelay.TotalMilliseconds);
        entity.RetryMaxDelayMilliseconds = checked((long)destination.RetryPolicy.MaxDelay.TotalMilliseconds);
    }

    private static string SerializeEvents(
        IEnumerable<PaymentRequestLifecycleEventKind> eventKinds) =>
        string.Join(
            ',',
            eventKinds
                .OrderBy(value => (int)value)
                .Select(value => ((int)value).ToString(System.Globalization.CultureInfo.InvariantCulture)));

    private static IReadOnlyList<PaymentRequestLifecycleEventKind> ParseEvents(string serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
            throw new InvalidOperationException("Persisted webhook destination has no lifecycle event subscriptions.");

        return serialized
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value =>
            {
                if (!int.TryParse(
                        value,
                        System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var numeric) ||
                    !Enum.IsDefined(typeof(PaymentRequestLifecycleEventKind), numeric))
                {
                    throw new InvalidOperationException(
                        "Persisted webhook destination contains an unsupported lifecycle event subscription.");
                }

                return (PaymentRequestLifecycleEventKind)numeric;
            })
            .ToArray();
    }
}
