using System.Globalization;
using AfriWallet.PaymentRequests.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.WebhookSubscriptions.Persistence;

public sealed class EfPaymentRequestWebhookDeliveryAttemptStore(
    PaymentRequestWebhookSubscriptionDbContext dbContext)
    : IPaymentRequestWebhookDeliveryAttemptStore
{
    public async Task AppendAsync(
        PaymentRequestWebhookDeliveryAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        dbContext.DeliveryAttempts.Add(Map(attempt));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentRequestWebhookDeliveryAttempt>> ListAsync(
        Guid subscriptionId,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Webhook subscription id cannot be empty.", nameof(subscriptionId));
        if (limit is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(limit));

        var entities = await dbContext.DeliveryAttempts.AsNoTracking()
            .Where(x => x.SubscriptionId == subscriptionId)
            .OrderByDescending(x => x.CompletedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return entities.Select(Map).ToArray();
    }

    public async Task<PaymentRequestWebhookDeliveryReliabilityMetrics> GetMetricsAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Webhook subscription id cannot be empty.", nameof(subscriptionId));

        var attempts = await dbContext.DeliveryAttempts.AsNoTracking()
            .Where(x => x.SubscriptionId == subscriptionId)
            .Select(x => new
            {
                x.Outcome,
                x.LatencyMilliseconds,
                x.CompletedAtUtc
            })
            .ToListAsync(cancellationToken);

        if (attempts.Count == 0)
            return PaymentRequestWebhookDeliveryReliabilityMetrics.Empty(subscriptionId);

        var successful = attempts.Count(x =>
            x.Outcome == (int)PaymentRequestWebhookDeliveryAttemptOutcome.Success);
        var transientFailures = attempts.Count(x =>
            x.Outcome == (int)PaymentRequestWebhookDeliveryAttemptOutcome.TransientFailure);
        var permanentFailures = attempts.Count(x =>
            x.Outcome == (int)PaymentRequestWebhookDeliveryAttemptOutcome.PermanentFailure);
        var failures = transientFailures + permanentFailures;

        var completed = attempts
            .Select(x => Parse(x.CompletedAtUtc))
            .OrderByDescending(x => x)
            .ToArray();

        var lastSuccess = attempts
            .Where(x => x.Outcome == (int)PaymentRequestWebhookDeliveryAttemptOutcome.Success)
            .Select(x => Parse(x.CompletedAtUtc))
            .OrderByDescending(x => x)
            .Cast<DateTimeOffset?>()
            .FirstOrDefault();

        return new PaymentRequestWebhookDeliveryReliabilityMetrics(
            subscriptionId,
            attempts.Count,
            successful,
            transientFailures,
            permanentFailures,
            (decimal)failures / attempts.Count,
            completed[0],
            lastSuccess,
            attempts.Average(x => (double)x.LatencyMilliseconds));
    }

    private static PaymentRequestWebhookDeliveryAttemptEntity Map(
        PaymentRequestWebhookDeliveryAttempt value) => new()
    {
        Id = value.Id,
        SubscriptionId = value.SubscriptionId,
        EventId = value.EventId,
        Outcome = (int)value.Outcome,
        HttpStatusCode = value.HttpStatusCode,
        LatencyMilliseconds = value.LatencyMilliseconds,
        StartedAtUtc = Format(value.StartedAtUtc),
        CompletedAtUtc = Format(value.CompletedAtUtc)
    };

    private static PaymentRequestWebhookDeliveryAttempt Map(
        PaymentRequestWebhookDeliveryAttemptEntity value) =>
        new(
            value.Id,
            value.SubscriptionId,
            value.EventId,
            (PaymentRequestWebhookDeliveryAttemptOutcome)value.Outcome,
            value.HttpStatusCode,
            value.LatencyMilliseconds,
            Parse(value.StartedAtUtc),
            Parse(value.CompletedAtUtc));

    private static string Format(DateTimeOffset value) =>
        value.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset Parse(string value) =>
        DateTimeOffset.ParseExact(
            value,
            "O",
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);
}
