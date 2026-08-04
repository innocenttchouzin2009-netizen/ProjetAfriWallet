using UniversalWallet.Api.Domain.Currency;

namespace UniversalWallet.Api.Domain.Fx;

public sealed class FxConversion
{
	public FxConversion(
		Guid conversionId,
		CurrencyAmount sourceAmount,
		CurrencyAmount targetAmount,
		ExchangeRate exchangeRate,
		CurrencyAmount fee,
		CurrencyAmount spread,
		DateTimeOffset timestamp,
		string provider)
	{
		ConversionId = conversionId;
		SourceAmount = sourceAmount;
		TargetAmount = targetAmount;
		ExchangeRate = exchangeRate;
		Fee = fee;
		Spread = spread;
		Timestamp = timestamp;
		Provider = provider;
	}

	public Guid ConversionId { get; }
	public CurrencyAmount SourceAmount { get; }
	public CurrencyAmount TargetAmount { get; }
	public ExchangeRate ExchangeRate { get; }
	public CurrencyAmount Fee { get; }
	public CurrencyAmount Spread { get; }
	public DateTimeOffset Timestamp { get; }
	public string Provider { get; }
}
