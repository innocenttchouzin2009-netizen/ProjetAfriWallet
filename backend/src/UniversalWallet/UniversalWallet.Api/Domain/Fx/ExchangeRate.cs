namespace UniversalWallet.Api.Domain.Fx;

public sealed class ExchangeRate
{
	public ExchangeRate(
		Guid id,
		string baseCurrency,
		string quoteCurrency,
		decimal rate,
		string provider,
		string? providerReference,
		DateTimeOffset validFrom,
		DateTimeOffset validUntil,
		ExchangeRateStatus status,
		long version,
		DateTimeOffset createdAt)
	{
		Id = id;
		BaseCurrency = baseCurrency;
		QuoteCurrency = quoteCurrency;
		Rate = rate;
		Provider = provider;
		ProviderReference = providerReference;
		ValidFrom = validFrom;
		ValidUntil = validUntil;
		Status = status;
		Version = version;
		CreatedAt = createdAt;
	}

	public Guid Id { get; }
	public string BaseCurrency { get; }
	public string QuoteCurrency { get; }
	public decimal Rate { get; }
	public string Provider { get; }
	public string? ProviderReference { get; }
	public DateTimeOffset ValidFrom { get; }
	public DateTimeOffset ValidUntil { get; }
	public ExchangeRateStatus Status { get; }
	public long Version { get; }
	public DateTimeOffset CreatedAt { get; }
}
