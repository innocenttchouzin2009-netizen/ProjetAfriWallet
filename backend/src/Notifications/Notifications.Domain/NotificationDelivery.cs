namespace AfriWallet.Notifications.Domain;

public sealed class NotificationDelivery
{
    private NotificationDelivery(
        Guid id,
        Guid eventId,
        Guid userId,
        NotificationChannel channel,
        string kind,
        string title,
        string body,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset routedAtUtc,
        IReadOnlyDictionary<string, string> data)
    {
        Id = id;
        EventId = eventId;
        UserId = userId;
        Channel = channel;
        Kind = kind;
        Title = title;
        Body = body;
        OccurredAtUtc = occurredAtUtc;
        RoutedAtUtc = routedAtUtc;
        Data = data;
    }

    public Guid Id { get; }
    public Guid EventId { get; }
    public Guid UserId { get; }
    public NotificationChannel Channel { get; }
    public string Kind { get; }
    public string Title { get; }
    public string Body { get; }
    public DateTimeOffset OccurredAtUtc { get; }
    public DateTimeOffset RoutedAtUtc { get; }
    public IReadOnlyDictionary<string, string> Data { get; }

    public static NotificationDelivery Create(
        ActivityEvent activityEvent,
        NotificationChannel channel,
        DateTimeOffset routedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(activityEvent);
        if (!Enum.IsDefined(channel))
            throw new ArgumentOutOfRangeException(nameof(channel), "Notification channel is invalid.");
        if (routedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Routing timestamp must be UTC.", nameof(routedAtUtc));
        if (routedAtUtc < activityEvent.OccurredAtUtc)
            throw new ArgumentException("Routing timestamp cannot precede the activity event.", nameof(routedAtUtc));

        return new NotificationDelivery(
            Guid.NewGuid(),
            activityEvent.EventId,
            activityEvent.UserId,
            channel,
            activityEvent.Kind,
            activityEvent.Title,
            activityEvent.Body,
            activityEvent.OccurredAtUtc,
            routedAtUtc,
            new Dictionary<string, string>(activityEvent.Data, StringComparer.Ordinal));
    }
}
