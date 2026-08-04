using MobileMoney.Production.Configuration;
using MobileMoney.Production.Secrets;

namespace MobileMoney.Production.Diagnostics;

public sealed class ConfigurationDiagnosticService
{
    private readonly MtnMomoProductionOptions _options;
    private readonly ISecretProvider _secretProvider;

    public ConfigurationDiagnosticService(MtnMomoProductionOptions options, ISecretProvider secretProvider)
    {
        _options = options;
        _secretProvider = secretProvider;
    }

    public object GetDiagnosticSnapshot()
    {
        return new
        {
            environment = _options.Environment,
            enableProduction = _options.EnableProduction,
            baseUrl = _options.BaseUrl,
            timeoutSeconds = _options.TimeoutSeconds,
            secretsConfigured = new
            {
                apiUser = _secretProvider.HasSecret(_options.ApiUserSecretName),
                apiKey = _secretProvider.HasSecret(_options.ApiKeySecretName),
                subscriptionKey = _secretProvider.HasSecret(_options.SubscriptionKeySecretName),
                callbackSecret = _secretProvider.HasSecret(_options.CallbackSecretName)
            }
        };
    }
}
