namespace AfriWallet.Notifications.Domain;

public sealed class ActivityEvent
{
    private ActivityEvent(
        Guid eventId,
        Guid userId,
        string kind,
        string title,
        string body,
        DateTimeOffset occurredAtUtc,
        IReadOnlyDictionary<string, string> data)
    {
        EventId = eventId;
        UserId = userId;
        Kind = kind;
        Title = title;
        Body = body;
        OccurredAtUtc = occurredAtUtc;
        Data = data;
    }

    public Guid EventId { get; }
    public Guid UserId { get; }
    public string Kind { get; }
    public string Title { get; }
    public string Body { get; }
    public DateTimeOffset OccurredAtUtc { get; }
    public IReadOnlyDictionary<string, string> Data { get; }

    public static ActivityEvent Create(
        Guid userId,
        string kind,
        string title,
        string body,
        DateTimeOffset occurredAtUtc,
        IReadOnlyDictionary<string, string>? data = null) =>
        Restore(Guid.NewGuid(), userId, kind, title, body, occurredAtUtc, data);

    public static ActivityEvent Restore(
        Guid eventId,
        Guid userId,
        string kind,
        string title,
        string body,
        DateTimeOffset occurredAtUtc,
        IReadOnlyDictionary<string, string>? data = null)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("Activity event id cannot be empty.", nameof(eventId));
        if (userId == Guid.Empty)
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        if (string.IsNullOrWhiteSpace(kind))
            throw new ArgumentException("Activity event kind is required.", nameof(kind));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Activity event title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Activity event body is required.", nameof(body));
        if (kind.Trim().Length > 128)
            throw new ArgumentException("Activity event kind cannot exceed 128 characters.", nameof(kind));
        if (title.Trim().Length > 160)
            throw new ArgumentException("Activity event title cannot exceed 160 characters.", nameof(title));
        if (body.Trim().Length > 2048)
            throw new ArgumentException("Activity event body cannot exceed 2048 characters.", nameof(body));
        if (occurredAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Activity event timestamp must be UTC.", nameof(occurredAtUtc));

        var normalizedData = new Dictionary<string, string>(StringComparer.Ordinal);
        if (data is not null)
        {
            foreach (var pair in data)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    throw new ArgumentException("Activity event data keys cannot be empty.", nameof(data));
                if (pair.Key.Length > 128)
                    throw new ArgumentException("Activity event data keys cannot exceed 128 characters.", nameof(data));
                if (pair.Value is null)
                    throw new ArgumentException("Activity event data values cannot be null.", nameof(data));
                if (pair.Value.Length > 2048)
                    throw new ArgumentException("Activity event data values cannot exceed 2048 characters.", nameof(data));
                normalizedData.Add(pair.Key, pair.Value);
            }
        }

        return new ActivityEvent(
            eventId,
            userId,
            kind.Trim(),
            title.Trim(),
            body.Trim(),
            occurredAtUtc,
            normalizedData);
    }
}
