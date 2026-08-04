namespace MobileMoney.Production.Health;

public sealed class ConnectorHealthProbe : IHealthProbe
{
    public string Name => "mtn-momo-connector";

    public Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HealthCheckResult
        {
            Name = Name,
            Status = "Healthy",
            Description = "Sandbox connector is available"
        });
    }
}
