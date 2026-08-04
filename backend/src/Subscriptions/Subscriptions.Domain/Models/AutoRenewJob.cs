namespace Subscriptions.Domain.Models;

public enum AutoRenewJobStatus
{
    Scheduled,
    Processing,
    Succeeded,
    Failed,
    GracePeriod,
    Cancelled
}

public sealed class AutoRenewJob
{
    public string JobId { get; set; } = Guid.NewGuid().ToString("N");
    public string SubscriptionId { get; set; } = string.Empty;
    public DateTimeOffset ScheduledFor { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public AutoRenewJobStatus Status { get; set; } = AutoRenewJobStatus.Scheduled;
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<AutoRenewAttempt> Attempts { get; set; } = new();
}

public sealed class AutoRenewAttempt
{
    public string AttemptId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset AttemptedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Message { get; set; }
    public bool Succeeded { get; set; }
}
