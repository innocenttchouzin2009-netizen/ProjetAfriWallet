using UniversalWallet.Api.Domain.Fx;
using UniversalWallet.Api.Domain.FxQuotes;

namespace UniversalWallet.Api.Application.FxQuotes;

public sealed class QuoteCalculator
{
	private readonly SpreadPolicy _spreadPolicy;
	private readonly FeePolicy _feePolicy;

	public QuoteCalculator(SpreadPolicy? spreadPolicy = null, FeePolicy? feePolicy = null)
	{
		_spreadPolicy = spreadPolicy ?? new PercentageSpreadPolicy();
		_feePolicy = feePolicy ?? new FlatFeePolicy(1m);
	}

	public FxQuote CreateQuote(
		string fromCurrency,
		string toCurrency,
		long amountMinor,
		ExchangeRate exchangeRate,
		decimal spreadPercentage,
		decimal trustScore,
		string provider,
		DateTimeOffset? createdAt = null)
	{
		if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
		{
			var quoteCreatedAt = createdAt ?? DateTimeOffset.UtcNow;
			return new FxQuote(
				Guid.CreateVersion7(),
				fromCurrency,
				toCurrency,
				amountMinor,
				amountMinor,
				exchangeRate,
				0m,
				0m,
				0m,
				provider,
				trustScore,
				quoteCreatedAt.AddSeconds(30),
				quoteCreatedAt,
				QuoteStatus.Created);
		}

		var effectiveRate = _spreadPolicy.Apply(exchangeRate.Rate, spreadPercentage);
		var targetAmount = (decimal)amountMinor * effectiveRate;
		var feeMinor = _feePolicy.Calculate(amountMinor, (decimal)amountMinor * effectiveRate, exchangeRate.Rate);
		var roundedTarget = Math.Round(targetAmount - feeMinor, MidpointRounding.ToEven);
		var created = createdAt ?? DateTimeOffset.UtcNow;
		return new FxQuote(
			Guid.CreateVersion7(),
			fromCurrency,
			toCurrency,
			amountMinor,
			(long)roundedTarget,
			exchangeRate,
			spreadPercentage,
			feeMinor,
			feeMinor,
			provider,
			trustScore,
			created.AddSeconds(30),
			created,
			QuoteStatus.Created);
	}
}
