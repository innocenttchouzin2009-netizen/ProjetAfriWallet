namespace UniversalWallet.Api.Domain.Fx;

public sealed class FxRateHistory
{
	public FxRateHistory(
		Guid exchangeRateId,
		string baseCurrency,
		string quoteCurrency,
		decimal rate,
		string provider,
		DateTimeOffset recordedAt)
	{
		ExchangeRateId = exchangeRateId;
		BaseCurrency = baseCurrency;
		QuoteCurrency = quoteCurrency;
		Rate = rate;
		Provider = provider;
		RecordedAt = recordedAt;
	}

	public Guid ExchangeRateId { get; }
	public string BaseCurrency { get; }
	public string QuoteCurrency { get; }
	public decimal Rate { get; }
	public string Provider { get; }
	public DateTimeOffset RecordedAt { get; }
}
