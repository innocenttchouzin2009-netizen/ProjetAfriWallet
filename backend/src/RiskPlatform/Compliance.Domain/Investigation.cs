namespace Compliance.Domain;

public sealed class Investigation
{
    public Guid InvestigationId { get; init; } = Guid.NewGuid();
    public string Summary { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
