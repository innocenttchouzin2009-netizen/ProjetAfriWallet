namespace Compliance.Domain;

public sealed class ComplianceAlert
{
    public Guid AlertId { get; init; } = Guid.NewGuid();
    public string Source { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string Severity { get; set; } = "MEDIUM";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
