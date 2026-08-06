namespace Compliance.Domain;

public sealed class Evidence
{
    public Guid EvidenceId { get; init; } = Guid.NewGuid();
    public string Label { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
