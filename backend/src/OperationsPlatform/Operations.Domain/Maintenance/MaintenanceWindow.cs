namespace Operations.Domain;

public sealed class MaintenanceWindow
{
    public Guid WindowId { get; init; } = Guid.NewGuid();

    public DateTimeOffset StartUtc { get; init; }

    public DateTimeOffset EndUtc { get; init; }

    public string Reason { get; init; } = string.Empty;

    public List<string> Services { get; init; } = new();

    public string ApprovedBy { get; init; } = string.Empty;

    public bool IsActive(DateTimeOffset utcNow) => utcNow >= StartUtc && utcNow <= EndUtc;
}