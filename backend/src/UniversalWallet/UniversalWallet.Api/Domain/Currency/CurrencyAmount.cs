namespace UniversalWallet.Api.Domain.Currency;

public sealed class CurrencyAmount
{
	private CurrencyAmount(long minorValue, string currencyCode)
	{
		MinorValue = minorValue;
		CurrencyCode = currencyCode;
	}

	public long MinorValue { get; }
	public string CurrencyCode { get; }

	public static CurrencyAmount FromMinor(string currencyCode, long minorValue)
	{
		if (string.IsNullOrWhiteSpace(currencyCode))
		{
			throw new ArgumentException("Currency code is required.", nameof(currencyCode));
		}

		return new CurrencyAmount(minorValue, currencyCode.Trim().ToUpperInvariant());
	}

	public static CurrencyAmount FromMajor(string currencyCode, decimal majorValue, byte minorUnits, MidpointRounding roundingMode = MidpointRounding.ToEven)
	{
		var multiplier = Pow10(minorUnits);
		var minorValue = decimal.Round(majorValue * multiplier, 0, roundingMode);
		if (minorValue > long.MaxValue || minorValue < long.MinValue)
		{
			throw new OverflowException("Currency amount is out of range.");
		}

		return new CurrencyAmount((long)minorValue, currencyCode.Trim().ToUpperInvariant());
	}

	public decimal ToMajor(byte minorUnits)
	{
		return MinorValue / Pow10(minorUnits);
	}

	private static decimal Pow10(byte minorUnits)
	{
		decimal value = 1m;
		for (var index = 0; index < minorUnits; index++)
		{
			value *= 10m;
		}

		return value;
	}
}
