using UniversalWallet.Api.Domain.Currency;

namespace UniversalWallet.Api.Application.Fx;

public sealed class CurrencyResponse
{
	public string Code { get; init; } = string.Empty;
	public int NumericCode { get; init; }
	public string Name { get; init; } = string.Empty;
	public byte MinorUnits { get; init; }
	public string Symbol { get; init; } = string.Empty;
	public string Region { get; init; } = string.Empty;
	public CurrencyStatus Status { get; init; }
	public DateTimeOffset CreatedAt { get; init; }
}
