namespace Support.Domain;

public sealed class SupportTimelineEntry
{
    public Guid EntryId { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string ActorId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
