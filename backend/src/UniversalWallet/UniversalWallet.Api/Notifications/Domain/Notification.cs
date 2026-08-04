namespace UniversalWallet.Api.Notifications.Domain;

public enum NotificationChannel
{
    InApp,
    Push,
    Email,
    Sms,
    Webhook
}

public enum NotificationStatus
{
    Created,
    Queued,
    Sending,
    Sent,
    Delivered,
    Read,
    Failed,
    Cancelled
}

public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Critical
}

public sealed class Notification
{
    public Guid NotificationId { get; init; } = Guid.CreateVersion7();
    public Guid UserAwid { get; init; }
    public string EventType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; } = NotificationStatus.Created;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ScheduledAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string NotificationKey { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
}
