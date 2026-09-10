using AfriWallet.Fx.Domain;

namespace AfriWallet.Fx.Application;

public sealed class FxConversionService
{
    public const byte MaximumMinorUnitDigits = 6;
    public const string RoundingModeName = "ToEven";

    public FxConversionResult Convert(FxConversionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.SourceAmountMinor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "Source amount must not be negative.");
        }

        ValidateMinorUnitDigits(command.SourceMinorUnitDigits, nameof(command.SourceMinorUnitDigits));
        ValidateMinorUnitDigits(command.TargetMinorUnitDigits, nameof(command.TargetMinorUnitDigits));

        var sourceCurrency = CurrencyCode.Create(command.SourceCurrencyCode);
        var targetCurrency = CurrencyCode.Create(command.TargetCurrencyCode);
        var pair = new CurrencyPair(sourceCurrency, targetCurrency);
        var quote = new FxRateQuote(pair, command.Rate, command.QuotedAtUtc);

        var sourceScale = Pow10(command.SourceMinorUnitDigits);
        var targetScale = Pow10(command.TargetMinorUnitDigits);

        decimal targetMinorExact;
        try
        {
            targetMinorExact = checked((command.SourceAmountMinor / sourceScale) * quote.Rate * targetScale);
        }
        catch (OverflowException)
        {
            throw new OverflowException("FX conversion exceeded decimal range.");
        }

        var rounded = decimal.Round(targetMinorExact, 0, MidpointRounding.ToEven);
        if (rounded > long.MaxValue)
        {
            throw new OverflowException("FX conversion exceeded minor-unit range.");
        }

        return new FxConversionResult(
            sourceCurrency.Value,
            targetCurrency.Value,
            command.SourceAmountMinor,
            decimal.ToInt64(rounded),
            command.SourceMinorUnitDigits,
            command.TargetMinorUnitDigits,
            quote.Rate,
            quote.QuotedAtUtc,
            RoundingModeName);
    }

    private static void ValidateMinorUnitDigits(byte digits, string parameterName)
    {
        if (digits > MaximumMinorUnitDigits)
        {
            throw new ArgumentOutOfRangeException(parameterName, $"Minor-unit digits must be between 0 and {MaximumMinorUnitDigits}.");
        }
    }

    private static decimal Pow10(byte exponent)
    {
        var result = 1m;
        for (var index = 0; index < exponent; index++)
        {
            result *= 10m;
        }

        return result;
    }
}
