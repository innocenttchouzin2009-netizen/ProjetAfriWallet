namespace AfriWallet.Banking.Api.Production.Configuration;

public sealed class BankingProductionConfigurationService
{
    private readonly IConfiguration _configuration;

    public BankingProductionConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IReadOnlyDictionary<string, string?> GetSummary()
    {
        return new Dictionary<string, string?>
        {
            ["Banking:Production:Enabled"] = _configuration["Banking:Production:Enabled"],
            ["Banking:Production:Environment"] = _configuration["Banking:Production:Environment"],
            ["Banking:Production:RequiredSettings"] = string.Join(",", _configuration.GetSection("Banking:Production:RequiredSettings").GetChildren().Select(x => x.Value ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x))),
            ["Banking:Production:RequiredSecrets"] = string.Join(",", _configuration.GetSection("Banking:Production:RequiredSecrets").GetChildren().Select(x => x.Value ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)))
        };
    }
}
