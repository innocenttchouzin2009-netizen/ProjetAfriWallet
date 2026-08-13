namespace Support.Domain;

public sealed class SupportAssignment
{
    public Guid AssignmentId { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public string Team { get; set; } = string.Empty;
    public string? AgentId { get; set; }
    public bool IsAutomatic { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
