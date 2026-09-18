using System.Globalization;
using System.Text.Json;
using AfriWallet.Webhooks.Domain;

namespace AfriWallet.Webhooks.Persistence;

internal static class WebhookSubscriptionEntityMapper
{
    public static WebhookSubscriptionEntity ToEntity(WebhookSubscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        return new WebhookSubscriptionEntity
        {
            Id = subscription.Id.Value,
            OwnerId = subscription.OwnerId,
            Endpoint = subscription.Endpoint.AbsoluteUri,
            EventTypesJson = JsonSerializer.Serialize(
                subscription.EventTypes.Select(eventType => eventType.Value).ToArray()),
            SigningKeyReference = subscription.SigningKeyReference,
            Status = (int)subscription.Status,
            CreatedAtUtc = Format(subscription.CreatedAtUtc),
            UpdatedAtUtc = Format(subscription.UpdatedAtUtc)
        };
    }

    public static void Apply(WebhookSubscriptionEntity entity, WebhookSubscription subscription)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(subscription);

        if (entity.Id != subscription.Id.Value)
            throw new InvalidOperationException("Webhook subscription identity cannot change.");

        entity.OwnerId = subscription.OwnerId;
        entity.Endpoint = subscription.Endpoint.AbsoluteUri;
        entity.EventTypesJson = JsonSerializer.Serialize(
            subscription.EventTypes.Select(eventType => eventType.Value).ToArray());
        entity.SigningKeyReference = subscription.SigningKeyReference;
        entity.Status = (int)subscription.Status;
        entity.CreatedAtUtc = Format(subscription.CreatedAtUtc);
        entity.UpdatedAtUtc = Format(subscription.UpdatedAtUtc);
    }

    public static WebhookSubscription ToDomain(WebhookSubscriptionEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        string[] eventTypeValues;
        try
        {
            eventTypeValues = JsonSerializer.Deserialize<string[]>(entity.EventTypesJson)
                ?? throw new InvalidOperationException("Persisted webhook event types are missing.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Persisted webhook event types are invalid.", exception);
        }

        if (!Enum.IsDefined(typeof(WebhookSubscriptionStatus), entity.Status))
            throw new InvalidOperationException("Persisted webhook subscription status is invalid.");

        return WebhookSubscription.Restore(
            WebhookSubscriptionId.From(entity.Id),
            entity.OwnerId,
            new Uri(entity.Endpoint, UriKind.Absolute),
            eventTypeValues.Select(WebhookEventType.Create),
            entity.SigningKeyReference,
            (WebhookSubscriptionStatus)entity.Status,
            Parse(entity.CreatedAtUtc, nameof(entity.CreatedAtUtc)),
            Parse(entity.UpdatedAtUtc, nameof(entity.UpdatedAtUtc)));
    }

    private static string Format(DateTimeOffset value) =>
        value.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset Parse(string value, string fieldName)
    {
        if (!DateTimeOffset.TryParseExact(
                value,
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed))
        {
            throw new InvalidOperationException($"Persisted {fieldName} is invalid.");
        }

        return parsed;
    }
}
