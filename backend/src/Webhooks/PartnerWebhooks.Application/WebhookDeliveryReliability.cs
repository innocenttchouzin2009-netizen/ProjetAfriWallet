using System.Net;
using AfriWallet.PartnerWebhooks.Domain;

namespace AfriWallet.PartnerWebhooks.Application;

public enum WebhookDeliveryFailureKind
{
    Transient = 1,
    Permanent = 2
}

public enum WebhookDeliveryAttemptOutcome
{
    Succeeded = 1,
    TransientFailure = 2,
    PermanentFailure = 3
}

public sealed record WebhookDeliveryAttempt(
    Guid AttemptId,
    PartnerWebhookSubscriptionId SubscriptionId,
    WebhookEventId EventId,
    int AttemptNumber,
    WebhookDeliveryAttemptOutcome Outcome,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    int? HttpStatusCode,
    string? FailureCode,
    DateTimeOffset? NextRetryAtUtc);

public sealed record ReliableWebhookDeliveryResult(
    bool Delivered,
    bool AlreadyDelivered,
    WebhookDeliveryAttempt? Attempt);

public interface IWebhookDeliveryAttemptRepository
{
    Task<bool> HasSucceededAsync(
        PartnerWebhookSubscriptionId subscriptionId,
        WebhookEventId eventId,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        PartnerWebhookSubscriptionId subscriptionId,
        WebhookEventId eventId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        WebhookDeliveryAttempt attempt,
        CancellationToken cancellationToken = default);
}

public static class WebhookDeliveryFailureClassifier
{
    public static WebhookDeliveryFailureKind Classify(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is HttpRequestException http)
        {
            if (http.StatusCode is null)
            {
                return WebhookDeliveryFailureKind.Transient;
            }

            var code = (int)http.StatusCode.Value;
            if (code == (int)HttpStatusCode.RequestTimeout ||
                code == 429 ||
                code >= 500)
            {
                return WebhookDeliveryFailureKind.Transient;
            }

            return WebhookDeliveryFailureKind.Permanent;
        }

        if (exception is TimeoutException)
        {
            return WebhookDeliveryFailureKind.Transient;
        }

        return WebhookDeliveryFailureKind.Permanent;
    }
}

public sealed class WebhookRetryBackoffPolicy(
    TimeSpan? baseDelay = null,
    TimeSpan? maxDelay = null)
{
    private readonly TimeSpan _baseDelay = baseDelay ?? TimeSpan.FromSeconds(30);
    private readonly TimeSpan _maxDelay = maxDelay ?? TimeSpan.FromMinutes(30);

    public TimeSpan GetDelay(int attemptNumber)
    {
        if (attemptNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptNumber));
        }

        var factor = Math.Pow(2, Math.Min(attemptNumber - 1, 20));
        var ticks = Math.Min(_baseDelay.Ticks * factor, _maxDelay.Ticks);
        return TimeSpan.FromTicks((long)ticks);
    }
}

public sealed class ReliableSubscriptionWebhookDeliveryService(
    ISubscriptionWebhookDeliveryPort deliveryPort,
    IWebhookDeliveryAttemptRepository attemptRepository,
    WebhookRetryBackoffPolicy backoffPolicy)
{
    public async Task<ReliableWebhookDeliveryResult> DeliverOnceAsync(
        SubscriptionWebhookDelivery delivery,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        if (startedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Delivery attempt timestamp must be UTC.", nameof(startedAtUtc));
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (await attemptRepository.HasSucceededAsync(
                delivery.SubscriptionId,
                delivery.Event.EventId,
                cancellationToken))
        {
            return new ReliableWebhookDeliveryResult(
                Delivered: true,
                AlreadyDelivered: true,
                Attempt: null);
        }

        var attemptNumber = await attemptRepository.CountAsync(
            delivery.SubscriptionId,
            delivery.Event.EventId,
            cancellationToken) + 1;

        try
        {
            await deliveryPort.DeliverAsync(delivery, cancellationToken);

            var completedAtUtc = DateTimeOffset.UtcNow;
            var success = new WebhookDeliveryAttempt(
                Guid.NewGuid(),
                delivery.SubscriptionId,
                delivery.Event.EventId,
                attemptNumber,
                WebhookDeliveryAttemptOutcome.Succeeded,
                startedAtUtc,
                completedAtUtc,
                null,
                null,
                null);

            await attemptRepository.AddAsync(success, cancellationToken);

            return new ReliableWebhookDeliveryResult(
                Delivered: true,
                AlreadyDelivered: false,
                Attempt: success);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var completedAtUtc = DateTimeOffset.UtcNow;
            var kind = WebhookDeliveryFailureClassifier.Classify(exception);
            var outcome = kind == WebhookDeliveryFailureKind.Transient
                ? WebhookDeliveryAttemptOutcome.TransientFailure
                : WebhookDeliveryAttemptOutcome.PermanentFailure;

            DateTimeOffset? nextRetryAtUtc = kind == WebhookDeliveryFailureKind.Transient
                ? completedAtUtc.Add(backoffPolicy.GetDelay(attemptNumber))
                : null;

            var httpStatusCode = exception is HttpRequestException http && http.StatusCode is not null
                ? (int?)http.StatusCode.Value
                : null;

            var attempt = new WebhookDeliveryAttempt(
                Guid.NewGuid(),
                delivery.SubscriptionId,
                delivery.Event.EventId,
                attemptNumber,
                outcome,
                startedAtUtc,
                completedAtUtc,
                httpStatusCode,
                exception.GetType().Name,
                nextRetryAtUtc);

            await attemptRepository.AddAsync(attempt, cancellationToken);

            return new ReliableWebhookDeliveryResult(
                Delivered: false,
                AlreadyDelivered: false,
                Attempt: attempt);
        }
    }
}
