using MobileMoney.Production.Configuration;
using MobileMoney.Production.Secrets;

namespace MobileMoney.Production.Health;

public sealed class SecretProviderHealthProbe : IHealthProbe
{
    private readonly MtnMomoProductionOptions _options;
    private readonly ISecretProvider _secretProvider;

    public SecretProviderHealthProbe(MtnMomoProductionOptions options, ISecretProvider secretProvider)
    {
        _options = options;
        _secretProvider = secretProvider;
    }

    public string Name => "mtn-momo-secret-provider";

    public Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var hasRequiredSecrets = _secretProvider.HasSecret(_options.ApiUserSecretName)
            && _secretProvider.HasSecret(_options.ApiKeySecretName)
            && _secretProvider.HasSecret(_options.SubscriptionKeySecretName)
            && _secretProvider.HasSecret(_options.CallbackSecretName);

        return Task.FromResult(new HealthCheckResult
        {
            Name = Name,
            Status = hasRequiredSecrets ? "Healthy" : "Degraded",
            Description = hasRequiredSecrets ? "Secrets available" : "Missing one or more configured secrets"
        });
    }
}
