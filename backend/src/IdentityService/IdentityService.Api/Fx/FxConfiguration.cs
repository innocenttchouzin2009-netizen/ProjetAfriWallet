using System.Globalization;
using AfriWallet.Fx.Infrastructure;

namespace IdentityService.Api.Fx;

public static class FxConfiguration
{
    public static IReadOnlyList<ConfiguredFxRate> LoadRates(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var rates = new List<ConfiguredFxRate>();
        foreach (var rateSection in configuration.GetSection("Fx:Rates").GetChildren())
        {
            var sourceCurrencyCode = rateSection["SourceCurrencyCode"];
            var targetCurrencyCode = rateSection["TargetCurrencyCode"];
            var rateText = rateSection["Rate"];

            if (string.IsNullOrWhiteSpace(sourceCurrencyCode) ||
                string.IsNullOrWhiteSpace(targetCurrencyCode) ||
                !decimal.TryParse(rateText, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate))
            {
                throw new InvalidOperationException(
                    "Each Fx:Rates entry must define SourceCurrencyCode, TargetCurrencyCode and a valid invariant decimal Rate.");
            }

            rates.Add(new ConfiguredFxRate(sourceCurrencyCode, targetCurrencyCode, rate));
        }

        return rates;
    }
}
