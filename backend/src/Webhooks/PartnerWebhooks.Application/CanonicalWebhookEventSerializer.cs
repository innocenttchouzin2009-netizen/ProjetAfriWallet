using System.Buffers;
using System.Text;
using System.Text.Json;

namespace AfriWallet.PartnerWebhooks.Application;

public interface IWebhookEventSerializer
{
    string Serialize(OutboundWebhookEvent webhookEvent);
}

public sealed class CanonicalWebhookEventSerializer : IWebhookEventSerializer
{
    public string Serialize(OutboundWebhookEvent webhookEvent)
    {
        ArgumentNullException.ThrowIfNull(webhookEvent);

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteString("eventId", webhookEvent.EventId.Value.ToString("D"));
            writer.WriteString("type", webhookEvent.EventType.Value);
            writer.WriteString("occurredAtUtc", webhookEvent.OccurredAtUtc.ToString("O"));
            writer.WritePropertyName("payload");
            WriteCanonicalJson(writer, webhookEvent.Payload);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void WriteCanonicalJson(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonicalJson(writer, property.Value);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteCanonicalJson(writer, item);
                }
                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;

            case JsonValueKind.Number:
                writer.WriteRawValue(element.GetRawText(), skipInputValidation: false);
                break;

            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;

            default:
                throw new InvalidOperationException("Unsupported JSON payload value.");
        }
    }
}
