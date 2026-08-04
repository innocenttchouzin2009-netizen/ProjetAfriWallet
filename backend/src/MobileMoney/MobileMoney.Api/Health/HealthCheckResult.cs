namespace MobileMoney.Production.Health;

public sealed class HealthCheckResult
{
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = "Healthy";
    public long DurationMs { get; init; }
    public string? Description { get; init; }
}

public sealed class HealthResponse
{
    public string Status { get; init; } = "Healthy";
    public List<HealthCheckResult> Checks { get; init; } = new();
}
