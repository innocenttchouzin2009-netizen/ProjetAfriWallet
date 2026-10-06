using AfriWallet.Fx.Application;
using MobileMoney.Production.Payout.Quote.Abstractions;

namespace MobileMoney.Production.Payout.Quote.Infrastructure;

public sealed class ConfiguredMobileMoneyPayoutCurrencyMinorUnitProvider
    : IMobileMoneyPayoutCurrencyMinorUnitProvider
{
    private readonly IReadOnlyDictionary<string, byte> _minorUnitDigits;

    public ConfiguredMobileMoneyPayoutCurrencyMinorUnitProvider(
        IEnumerable<KeyValuePair<string, byte>> configuredCurrencies)
    {
        ArgumentNullException.ThrowIfNull(configuredCurrencies);

        var normalized = new Dictionary<string, byte>(StringComparer.Ordinal);
        foreach (var configuredCurrency in configuredCurrencies)
        {
            var currencyCode = NormalizeCurrencyCode(configuredCurrency.Key);
            if (configuredCurrency.Value > FxConversionService.MaximumMinorUnitDigits)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(configuredCurrencies),
                    $"Currency '{currencyCode}' minor-unit digits must be between 0 and {FxConversionService.MaximumMinorUnitDigits}.");
            }

            if (!normalized.TryAdd(currencyCode, configuredCurrency.Value))
            {
                throw new InvalidOperationException(
                    $"Minor-unit metadata is already configured for '{currencyCode}'.");
            }
        }

        _minorUnitDigits = normalized;
    }

    public byte GetMinorUnitDigits(string currencyCode)
    {
        var normalized = NormalizeCurrencyCode(currencyCode);
        if (_minorUnitDigits.TryGetValue(normalized, out var digits))
            return digits;

        throw new KeyNotFoundException(
            $"Minor-unit metadata is not configured for currency '{normalized}'.");
    }

    private static string NormalizeCurrencyCode(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("Currency code is required.", nameof(currencyCode));

        var normalized = currencyCode.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(character => character is < 'A' or > 'Z'))
        {
            throw new ArgumentException(
                "Currency code must contain exactly three ASCII letters.",
                nameof(currencyCode));
        }

        return normalized;
    }
}
