namespace Support.Domain;

public sealed class SupportEscalation
{
    public Guid EscalationId { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public string Level { get; set; } = "L2";
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset EscalatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
