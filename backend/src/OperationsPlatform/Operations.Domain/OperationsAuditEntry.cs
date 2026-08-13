namespace Operations.Domain;

public sealed class OperationsAuditEntry
{
    public Guid EntryId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public string ActorId { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty;
    public string SubjectId { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
