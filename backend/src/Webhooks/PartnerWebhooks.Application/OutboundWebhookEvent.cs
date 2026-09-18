using System.Text.Json;
using AfriWallet.PartnerWebhooks.Domain;

namespace AfriWallet.PartnerWebhooks.Application;

public sealed class OutboundWebhookEvent
{
    private OutboundWebhookEvent(
        WebhookEventId eventId,
        WebhookEventType eventType,
        DateTimeOffset occurredAtUtc,
        JsonElement payload)
    {
        EventId = eventId;
        EventType = eventType;
        OccurredAtUtc = occurredAtUtc;
        Payload = payload.Clone();
    }

    public WebhookEventId EventId { get; }
    public WebhookEventType EventType { get; }
    public DateTimeOffset OccurredAtUtc { get; }
    public JsonElement Payload { get; }

    public static OutboundWebhookEvent Create(
        WebhookEventId eventId,
        WebhookEventType eventType,
        DateTimeOffset occurredAtUtc,
        JsonElement payload)
    {
        if (eventId.Value == Guid.Empty)
        {
            throw new ArgumentException("Webhook event id cannot be empty.", nameof(eventId));
        }

        if (string.IsNullOrWhiteSpace(eventType.Value))
        {
            throw new ArgumentException("Webhook event type is required.", nameof(eventType));
        }

        if (occurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Webhook event timestamp must be UTC.", nameof(occurredAtUtc));
        }

        if (payload.ValueKind == JsonValueKind.Undefined)
        {
            throw new ArgumentException("Webhook event payload must be defined JSON.", nameof(payload));
        }

        return new OutboundWebhookEvent(eventId, eventType, occurredAtUtc, payload);
    }
}
