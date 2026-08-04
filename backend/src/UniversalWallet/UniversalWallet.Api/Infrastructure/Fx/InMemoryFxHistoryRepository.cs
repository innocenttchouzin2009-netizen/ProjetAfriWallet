using UniversalWallet.Api.Domain.Fx;

namespace UniversalWallet.Api.Infrastructure.Fx;

public sealed class InMemoryFxHistoryRepository
{
	private readonly object _sync = new();
	private readonly List<FxRateHistory> _history = [];

	public void Save(FxRateHistory entry)
	{
		lock (_sync)
		{
			_history.Add(entry);
		}
	}

	public IReadOnlyList<FxRateHistory> List()
	{
		lock (_sync)
		{
			return _history.OrderByDescending(entry => entry.RecordedAt).ToList();
		}
	}

	public IReadOnlyList<FxRateHistory> List(string baseCurrency, string quoteCurrency)
	{
		lock (_sync)
		{
			var normalizedBase = baseCurrency.Trim().ToUpperInvariant();
			var normalizedQuote = quoteCurrency.Trim().ToUpperInvariant();
			return _history.Where(entry => entry.BaseCurrency == normalizedBase && entry.QuoteCurrency == normalizedQuote)
				.OrderByDescending(entry => entry.RecordedAt)
				.ToList();
		}
	}
}
