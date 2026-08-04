using UniversalWallet.Api.Domain.Fx;

namespace UniversalWallet.Api.Domain.FxQuotes;

public sealed class FxQuote
{
	public FxQuote(
		Guid quoteId,
		string fromCurrency,
		string toCurrency,
		long sourceAmountMinor,
		long targetAmountMinor,
		ExchangeRate exchangeRate,
		decimal spread,
		decimal fee,
		decimal totalFeeMinor,
		string provider,
		decimal trustScore,
		DateTimeOffset expiresAt,
		DateTimeOffset createdAt,
		QuoteStatus status)
	{
		QuoteId = quoteId;
		FromCurrency = fromCurrency;
		ToCurrency = toCurrency;
		SourceAmountMinor = sourceAmountMinor;
		TargetAmountMinor = targetAmountMinor;
		ExchangeRate = exchangeRate;
		Spread = spread;
		Fee = fee;
		TotalFeeMinor = totalFeeMinor;
		Provider = provider;
		TrustScore = trustScore;
		ExpiresAt = expiresAt;
		CreatedAt = createdAt;
		Status = status;
	}

	public Guid QuoteId { get; }
	public string FromCurrency { get; }
	public string ToCurrency { get; }
	public long SourceAmountMinor { get; }
	public long TargetAmountMinor { get; }
	public ExchangeRate ExchangeRate { get; }
	public decimal Spread { get; }
	public decimal Fee { get; }
	public decimal TotalFeeMinor { get; }
	public string Provider { get; }
	public decimal TrustScore { get; }
	public DateTimeOffset ExpiresAt { get; }
	public DateTimeOffset CreatedAt { get; }
	public QuoteStatus Status { get; private set; }

	public void Accept()
	{
		if (Status == QuoteStatus.Consumed)
		{
			throw new InvalidOperationException("QUOTE_CONSUMED");
		}
		if (DateTimeOffset.UtcNow > ExpiresAt)
		{
			Status = QuoteStatus.Expired;
			throw new InvalidOperationException("QUOTE_EXPIRED");
		}
		if (Status != QuoteStatus.Created)
		{
			throw new InvalidOperationException("QUOTE_NOT_CREATABLE");
		}
		Status = QuoteStatus.Accepted;
	}

	public void Consume()
	{
		if (Status != QuoteStatus.Accepted)
		{
			throw new InvalidOperationException("QUOTE_NOT_ACCEPTED");
		}
		Status = QuoteStatus.Consumed;
	}

	public void Expire()
	{
		if (Status == QuoteStatus.Consumed || Status == QuoteStatus.Accepted)
		{
			return;
		}
		Status = QuoteStatus.Expired;
	}
}
