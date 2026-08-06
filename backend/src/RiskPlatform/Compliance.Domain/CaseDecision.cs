namespace Compliance.Domain;

public sealed class CaseDecision
{
    public Guid DecisionId { get; init; } = Guid.NewGuid();
    public string DecisionType { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
