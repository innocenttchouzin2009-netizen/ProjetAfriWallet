using AfriWallet.Notifications.Domain;

namespace AfriWallet.Notifications.Application;

public sealed record NotificationDelivery
{
    public Guid NotificationId { get; }
    public Guid EventId { get; }
    public Guid RecipientUserId { get; }
    public NotificationChannel Channel { get; }
    public string Type { get; }
    public string ResourceType { get; }
    public string ResourceId { get; }
    public string Title { get; }
    public string Body { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public string? DataJson { get; }

    private NotificationDelivery(
        Guid notificationId,
        Guid eventId,
        Guid recipientUserId,
        NotificationChannel channel,
        string type,
        string resourceType,
        string resourceId,
        string title,
        string body,
        DateTimeOffset createdAtUtc,
        string? dataJson)
    {
        NotificationId = notificationId;
        EventId = eventId;
        RecipientUserId = recipientUserId;
        Channel = channel;
        Type = type;
        ResourceType = resourceType;
        ResourceId = resourceId;
        Title = title;
        Body = body;
        CreatedAtUtc = createdAtUtc;
        DataJson = dataJson;
    }

    public static NotificationDelivery Create(
        Guid notificationId,
        ActivityEvent activityEvent,
        NotificationChannel channel,
        string title,
        string body,
        DateTimeOffset createdAtUtc,
        string? dataJson = null)
    {
        if (notificationId == Guid.Empty)
            throw new ArgumentException("Notification id cannot be empty.", nameof(notificationId));
        ArgumentNullException.ThrowIfNull(activityEvent);
        if (!Enum.IsDefined(channel))
            throw new ArgumentOutOfRangeException(nameof(channel), channel, "Unsupported notification channel.");
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Notification title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Notification body is required.", nameof(body));

        var normalizedTitle = title.Trim();
        if (normalizedTitle.Length > 160)
            throw new ArgumentException("Notification title must not exceed 160 characters.", nameof(title));
        if (body.Length > 4096)
            throw new ArgumentException("Notification body must not exceed 4096 characters.", nameof(body));
        if (createdAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Notification timestamp must be UTC.", nameof(createdAtUtc));

        return new NotificationDelivery(
            notificationId,
            activityEvent.EventId,
            activityEvent.RecipientUserId,
            channel,
            activityEvent.Type,
            activityEvent.Resource.Type,
            activityEvent.Resource.Id,
            normalizedTitle,
            body,
            createdAtUtc,
            dataJson ?? activityEvent.MetadataJson);
    }
}

public enum NotificationDeliveryFailureKind
{
    Transient = 1,
    Permanent = 2
}

public sealed class NotificationDeliveryException : Exception
{
    public NotificationDeliveryException(
        NotificationDeliveryFailureKind failureKind,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        if (!Enum.IsDefined(failureKind))
            throw new ArgumentOutOfRangeException(nameof(failureKind));

        FailureKind = failureKind;
    }

    public NotificationDeliveryFailureKind FailureKind { get; }
}

public interface INotificationDeliveryPort
{
    Task DeliverAsync(
        NotificationDelivery notification,
        CancellationToken cancellationToken = default);
}
