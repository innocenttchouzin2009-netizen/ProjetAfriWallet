using AfriWallet.Wallet.Application;

namespace IdentityService.Api.Wallet;

public sealed class ConfiguredSupportedCurrencyPolicy : ISupportedCurrencyPolicy
{
    private readonly HashSet<string> supportedCurrencies;

    public ConfiguredSupportedCurrencyPolicy(IConfiguration configuration)
    {
        var configured = configuration.GetSection("Wallet:SupportedCurrencies").Get<string[]>()
            ?? ["XAF", "EUR", "USD"];

        supportedCurrencies = configured
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim().ToUpperInvariant())
            .ToHashSet(StringComparer.Ordinal);
    }

    public bool IsSupported(string currencyCode) =>
        supportedCurrencies.Contains(currencyCode.Trim().ToUpperInvariant());
}
