using Microsoft.Extensions.Options;

namespace AfriWallet.Merchant.Api.Production;

public sealed class MerchantProductionConfigurationService
{
    private readonly MerchantProductionConfiguration _configuration;

    public MerchantProductionConfigurationService(IOptions<MerchantProductionConfiguration> options)
    {
        _configuration = options.Value;
    }

    public object GetSummary() => new
    {
        environment = _configuration.EnvironmentName,
        sandboxMode = _configuration.SandboxMode,
        productionMode = _configuration.ProductionMode,
        requiredSecrets = _configuration.RequiredSecrets,
        externalEndpoints = _configuration.ExternalEndpoints
    };
}
