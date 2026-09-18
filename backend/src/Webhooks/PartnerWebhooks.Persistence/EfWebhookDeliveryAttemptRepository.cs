using AfriWallet.PartnerWebhooks.Application;
using AfriWallet.PartnerWebhooks.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PartnerWebhooks.Persistence;

public sealed class EfWebhookDeliveryAttemptRepository(PartnerWebhookDbContext dbContext)
    : IWebhookDeliveryAttemptRepository
{
    public Task<bool> HasSucceededAsync(
        PartnerWebhookSubscriptionId subscriptionId,
        WebhookEventId eventId,
        CancellationToken cancellationToken = default) =>
        dbContext.DeliveryAttempts
            .AsNoTracking()
            .AnyAsync(
                x => x.SubscriptionId == subscriptionId.Value &&
                     x.EventId == eventId.Value &&
                     x.Outcome == (int)WebhookDeliveryAttemptOutcome.Succeeded,
                cancellationToken);

    public Task<int> CountAsync(
        PartnerWebhookSubscriptionId subscriptionId,
        WebhookEventId eventId,
        CancellationToken cancellationToken = default) =>
        dbContext.DeliveryAttempts
            .AsNoTracking()
            .CountAsync(
                x => x.SubscriptionId == subscriptionId.Value &&
                     x.EventId == eventId.Value,
                cancellationToken);

    public async Task AddAsync(
        WebhookDeliveryAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);

        var entity = new WebhookDeliveryAttemptEntity
        {
            AttemptId = attempt.AttemptId,
            SubscriptionId = attempt.SubscriptionId.Value,
            EventId = attempt.EventId.Value,
            AttemptNumber = attempt.AttemptNumber,
            Outcome = (int)attempt.Outcome,
            StartedAtUtc = attempt.StartedAtUtc.ToString("O"),
            CompletedAtUtc = attempt.CompletedAtUtc.ToString("O"),
            HttpStatusCode = attempt.HttpStatusCode,
            FailureCode = attempt.FailureCode,
            NextRetryAtUtc = attempt.NextRetryAtUtc?.ToString("O"),
            SuccessKey = attempt.Outcome == WebhookDeliveryAttemptOutcome.Succeeded
                ? $"{attempt.SubscriptionId.Value:D}:{attempt.EventId.Value:D}"
                : null
        };

        dbContext.DeliveryAttempts.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
