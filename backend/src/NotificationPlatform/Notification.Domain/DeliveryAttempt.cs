namespace Notification.Domain;

public sealed class DeliveryAttempt
{
    public int AttemptNumber { get; set; }
    public NotificationChannel Channel { get; set; }
    public DeliveryStatus Status { get; set; }
    public string? Provider { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public long DurationMs { get; set; }
}
