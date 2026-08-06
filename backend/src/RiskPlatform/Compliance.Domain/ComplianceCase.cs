namespace Compliance.Domain;

public sealed class ComplianceCase
{
    public Guid CaseId { get; init; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CaseStatus Status { get; set; } = CaseStatus.Open;
    public string AssignedInvestigator { get; set; } = string.Empty;
    public string Priority { get; set; } = "MEDIUM";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<ComplianceAlert> Alerts { get; set; } = new();
    public List<Investigation> Investigations { get; set; } = new();
    public List<Evidence> Evidence { get; set; } = new();
    public List<CaseDecision> Decisions { get; set; } = new();
    public List<InvestigatorNote> Notes { get; set; } = new();
    public List<string> AuditEvents { get; set; } = new();
}
