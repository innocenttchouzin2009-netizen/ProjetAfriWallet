using MobileMoney.Production.Configuration;
using MobileMoney.Production.Secrets;

namespace MobileMoney.Production.Health;

public sealed class ConfigurationHealthProbe : IHealthProbe
{
    private readonly MtnMomoProductionOptions _options;
    private readonly ISecretProvider _secretProvider;

    public ConfigurationHealthProbe(MtnMomoProductionOptions options, ISecretProvider secretProvider)
    {
        _options = options;
        _secretProvider = secretProvider;
    }

    public string Name => "mtn-momo-configuration";

    public Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var healthy = !string.IsNullOrWhiteSpace(_options.Environment)
            && Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out _)
            && _options.TimeoutSeconds > 0;

        return Task.FromResult(new HealthCheckResult
        {
            Name = Name,
            Status = healthy ? "Healthy" : "Unhealthy",
            Description = healthy ? "Configuration is valid" : "Configuration is invalid"
        });
    }
}
