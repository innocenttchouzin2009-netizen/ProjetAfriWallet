namespace Notification.Domain;

public sealed class Notification
{
    public Guid NotificationId { get; set; } = Guid.NewGuid();
    public string TemplateKey { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public NotificationRecipient Recipient { get; set; } = new();
    public List<NotificationChannel> RequestedChannels { get; set; } = new();
    public List<NotificationChannel> EffectiveChannels { get; set; } = new();
    public string Locale { get; set; } = "en";
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeliveredAt { get; set; }
    public List<DeliveryAttempt> Attempts { get; set; } = new();
    public List<string> AuditEvents { get; set; } = new();
}
