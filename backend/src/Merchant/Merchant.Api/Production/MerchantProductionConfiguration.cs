namespace AfriWallet.Merchant.Api.Production;

public sealed class MerchantProductionConfiguration
{
    public const string SectionName = "MerchantProduction";

    public bool SandboxMode { get; init; } = true;
    public bool ProductionMode { get; init; }
    public string EnvironmentName { get; init; } = "Development";
    public string[] RequiredSecrets { get; init; } = ["MERCHANT_SIGNING_KEY"];
    public string[] ExternalEndpoints { get; init; } = ["https://payment-gateway.local"];
}
