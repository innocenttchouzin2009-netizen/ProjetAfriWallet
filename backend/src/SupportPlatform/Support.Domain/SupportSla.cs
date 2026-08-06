namespace Support.Domain;

public sealed class SupportSla
{
    public string PolicyId { get; set; } = "default-v1";
    public TimeSpan FirstResponseTarget { get; set; }
    public TimeSpan ResolutionTarget { get; set; }
    public TimeSpan WarningLeadTime { get; set; } = TimeSpan.FromMinutes(10);
    public bool IsPaused { get; set; }
    public DateTimeOffset? PausedAtUtc { get; set; }
    public TimeSpan TotalPausedDuration { get; set; } = TimeSpan.Zero;
    public DateTimeOffset? FirstResponseAtUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public DateTimeOffset? FirstResponseBreachedAtUtc { get; set; }
    public DateTimeOffset? ResolutionBreachedAtUtc { get; set; }
    public List<string> Violations { get; set; } = new();
}
