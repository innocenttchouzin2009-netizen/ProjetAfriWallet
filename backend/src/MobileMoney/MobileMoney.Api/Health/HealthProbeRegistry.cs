using System.Diagnostics;

namespace MobileMoney.Production.Health;

public sealed class HealthProbeRegistry
{
    private readonly IReadOnlyList<IHealthProbe> _probes;

    public HealthProbeRegistry(IEnumerable<IHealthProbe> probes)
    {
        _probes = probes.ToList();
    }

    public async Task<HealthResponse> RunAsync(Func<IHealthProbe, bool> predicate, CancellationToken cancellationToken = default)
    {
        var results = new List<HealthCheckResult>();
        foreach (var probe in _probes.Where(predicate))
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await probe.CheckAsync(cancellationToken);
                var completedResult = new HealthCheckResult
                {
                    Name = result.Name,
                    Status = result.Status,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    Description = result.Description
                };
                results.Add(completedResult);
            }
            catch (Exception ex)
            {
                results.Add(new HealthCheckResult
                {
                    Name = probe.Name,
                    Status = "Unhealthy",
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    Description = ex.Message
                });
            }
        }

        var status = results.All(x => x.Status.Equals("Healthy", StringComparison.OrdinalIgnoreCase))
            ? "Healthy"
            : results.Any(x => x.Status.Equals("Unhealthy", StringComparison.OrdinalIgnoreCase))
                ? "Degraded"
                : "Healthy";

        return new HealthResponse { Status = status, Checks = results };
    }
}
