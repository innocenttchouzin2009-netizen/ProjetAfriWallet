using UniversalWallet.Api.Domain.Fx;

namespace UniversalWallet.Api.Infrastructure.Fx;

public sealed class InMemoryFxRateHistoryRepository : IFxRateHistoryRepository
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
			return _rates.Where(rate => rate.BaseCurrency == baseCurrency.Trim().ToUpperInvariant() && rate.QuoteCurrency == quoteCurrency.Trim().ToUpperInvariant())
				.OrderByDescending(rate => rate.CreatedAt)
				.ToList();
		}
	}
}
