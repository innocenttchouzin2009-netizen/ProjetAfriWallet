using System.Text.Json;

namespace AfriWallet.Notifications.Domain;

public sealed record ActivityResourceReference
{
    public string Type { get; }
    public string Id { get; }

    private ActivityResourceReference(string type, string id)
    {
        Type = type;
        Id = id;
    }

    public static ActivityResourceReference Create(string type, string id)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Activity resource type is required.", nameof(type));
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Activity resource id is required.", nameof(id));

        var normalizedType = type.Trim();
        var normalizedId = id.Trim();

        if (normalizedType.Length > 64)
            throw new ArgumentException("Activity resource type must not exceed 64 characters.", nameof(type));
        if (normalizedId.Length > 128)
            throw new ArgumentException("Activity resource id must not exceed 128 characters.", nameof(id));
        if (normalizedType.Any(char.IsWhiteSpace))
            throw new ArgumentException("Activity resource type cannot contain whitespace.", nameof(type));

        return new ActivityResourceReference(normalizedType, normalizedId);
    }
}

public sealed record ActivityEvent
{
    public Guid EventId { get; }
    public Guid RecipientUserId { get; }
    public Guid? ActorUserId { get; }
    public string Type { get; }
    public ActivityResourceReference Resource { get; }
    public DateTimeOffset OccurredAtUtc { get; }
    public Guid? CorrelationId { get; }
    public string? MetadataJson { get; }

    private ActivityEvent(
        Guid eventId,
        Guid recipientUserId,
        Guid? actorUserId,
        string type,
        ActivityResourceReference resource,
        DateTimeOffset occurredAtUtc,
        Guid? correlationId,
        string? metadataJson)
    {
        EventId = eventId;
        RecipientUserId = recipientUserId;
        ActorUserId = actorUserId;
        Type = type;
        Resource = resource;
        OccurredAtUtc = occurredAtUtc;
        CorrelationId = correlationId;
        MetadataJson = metadataJson;
    }

    public static ActivityEvent Create(
        Guid eventId,
        Guid recipientUserId,
        Guid? actorUserId,
        string type,
        ActivityResourceReference resource,
        DateTimeOffset occurredAtUtc,
        Guid? correlationId = null,
        string? metadataJson = null)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("Activity event id cannot be empty.", nameof(eventId));
        if (recipientUserId == Guid.Empty)
            throw new ArgumentException("Activity recipient user id cannot be empty.", nameof(recipientUserId));
        if (actorUserId == Guid.Empty)
            throw new ArgumentException("Activity actor user id cannot be empty when supplied.", nameof(actorUserId));
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Activity event type is required.", nameof(type));
        ArgumentNullException.ThrowIfNull(resource);

        var normalizedType = type.Trim();
        if (normalizedType.Length > 128)
            throw new ArgumentException("Activity event type must not exceed 128 characters.", nameof(type));
        if (normalizedType.Any(char.IsWhiteSpace))
            throw new ArgumentException("Activity event type cannot contain whitespace.", nameof(type));
        if (occurredAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Activity event timestamp must be UTC.", nameof(occurredAtUtc));
        if (correlationId == Guid.Empty)
            throw new ArgumentException("Correlation id cannot be empty when supplied.", nameof(correlationId));

        var normalizedMetadata = NormalizeMetadata(metadataJson);

        return new ActivityEvent(
            eventId,
            recipientUserId,
            actorUserId,
            normalizedType,
            resource,
            occurredAtUtc,
            correlationId,
            normalizedMetadata);
    }

    public static ActivityEvent New(
        Guid recipientUserId,
        Guid? actorUserId,
        string type,
        ActivityResourceReference resource,
        DateTimeOffset occurredAtUtc,
        Guid? correlationId = null,
        string? metadataJson = null) =>
        Create(
            Guid.NewGuid(),
            recipientUserId,
            actorUserId,
            type,
            resource,
            occurredAtUtc,
            correlationId,
            metadataJson);

    private static string? NormalizeMetadata(string? metadataJson)
    {
        if (metadataJson is null)
            return null;
        if (string.IsNullOrWhiteSpace(metadataJson))
            throw new ArgumentException("Activity metadata JSON cannot be blank.", nameof(metadataJson));
        if (metadataJson.Length > 4096)
            throw new ArgumentException("Activity metadata JSON must not exceed 4096 characters.", nameof(metadataJson));

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new ArgumentException("Activity metadata JSON must be an object.", nameof(metadataJson));

            return document.RootElement.GetRawText();
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Activity metadata JSON is invalid.", nameof(metadataJson), exception);
        }
    }
}
