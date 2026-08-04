namespace MobileMoney.Production.Health;

public interface IHealthProbe
{
    string Name { get; }

    Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}
