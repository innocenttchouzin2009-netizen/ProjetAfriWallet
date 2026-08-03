using System.Text.RegularExpressions;

namespace UniversalWallet.Api.Domain.Currency;

public sealed class Currency
{
	private static readonly Regex CodePattern = new("^[A-Z]{3}$", RegexOptions.Compiled);

	public Currency(
		string code,
		int numericCode,
		string name,
		byte minorUnits,
		string symbol,
		string region,
		CurrencyStatus status,
		DateTimeOffset createdAt)
	{
		if (string.IsNullOrWhiteSpace(code))
		{
			throw new ArgumentException("CURRENCY_CODE_INVALID", nameof(code));
		}

		var normalizedCode = code.Trim().ToUpperInvariant();
		if (!CodePattern.IsMatch(normalizedCode))
		{
			throw new ArgumentException("CURRENCY_CODE_INVALID", nameof(code));
		}

		if (minorUnits > 4)
		{
			throw new ArgumentOutOfRangeException(nameof(minorUnits), "MINOR_UNITS_OUT_OF_RANGE");
		}

		Code = normalizedCode;
		NumericCode = numericCode;
		Name = name;
		MinorUnits = minorUnits;
		Symbol = symbol;
		Region = region;
		Status = status;
		CreatedAt = createdAt;
	}

	public string Code { get; }
	public int NumericCode { get; }
	public string Name { get; }
	public byte MinorUnits { get; }
	public string Symbol { get; }
	public string Region { get; }
	public CurrencyStatus Status { get; }
	public DateTimeOffset CreatedAt { get; }
}
