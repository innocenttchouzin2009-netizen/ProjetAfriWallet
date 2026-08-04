namespace MobileMoney.Production.Health;

public sealed class ReadinessHealthProbe : IHealthProbe
{
    public string Name => "mtn-momo-readiness";

    public Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HealthCheckResult
        {
            Name = Name,
            Status = "Healthy",
            Description = "Service can accept traffic"
        });
    }
}
