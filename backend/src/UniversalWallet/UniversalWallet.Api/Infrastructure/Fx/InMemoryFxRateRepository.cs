using UniversalWallet.Api.Domain.Fx;

namespace UniversalWallet.Api.Infrastructure.Fx;

public sealed class InMemoryFxRateRepository
{
	private readonly object _sync = new();
	private readonly List<ExchangeRate> _rates = [];

	public void Save(ExchangeRate rate)
	{
		lock (_sync)
		{
			_rates.Add(rate);
		}
	}

	public IReadOnlyList<ExchangeRate> List()
	{
		lock (_sync)
		{
			return _rates.OrderByDescending(rate => rate.CreatedAt).ToList();
		}
	}

	public IReadOnlyList<ExchangeRate> List(string baseCurrency, string quoteCurrency)
	{
		lock (_sync)
		{
			var normalizedBase = baseCurrency.Trim().ToUpperInvariant();
			var normalizedQuote = quoteCurrency.Trim().ToUpperInvariant();
			return _rates.Where(rate => rate.BaseCurrency == normalizedBase && rate.QuoteCurrency == normalizedQuote)
				.OrderByDescending(rate => rate.CreatedAt)
				.ToList();
		}
	}
}
