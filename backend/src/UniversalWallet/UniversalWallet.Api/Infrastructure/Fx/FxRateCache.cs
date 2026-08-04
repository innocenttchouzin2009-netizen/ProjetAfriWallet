using UniversalWallet.Api.Domain.Fx;

namespace UniversalWallet.Api.Infrastructure.Fx;

public sealed class FxRateCache
{
	private readonly object _sync = new();
	private readonly TimeSpan _ttl;
	private readonly Dictionary<(string BaseCurrency, string QuoteCurrency), FxRateCacheEntry> _entries = new();

	public FxRateCache(TimeSpan ttl)
	{
		_ttl = ttl;
	}

	public bool TryGet(string baseCurrency, string quoteCurrency, out ExchangeRate rate)
	{
		lock (_sync)
		{
			var key = (Normalize(baseCurrency), Normalize(quoteCurrency));
			if (_entries.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTimeOffset.UtcNow)
			{
				rate = entry.Rate;
				return true;
			}

			rate = null!;
			if (_entries.TryGetValue(key, out entry))
			{
				_entries.Remove(key);
			}

			return false;
		}
	}

	public void Set(ExchangeRate rate)
	{
		lock (_sync)
		{
			var key = (Normalize(rate.BaseCurrency), Normalize(rate.QuoteCurrency));
			var fetchedAt = DateTimeOffset.UtcNow;
			_entries[key] = new FxRateCacheEntry(rate, fetchedAt, fetchedAt.Add(_ttl));
		}
	}

	public void Remove(string baseCurrency, string quoteCurrency)
	{
		lock (_sync)
		{
			_entries.Remove((Normalize(baseCurrency), Normalize(quoteCurrency)));
		}
	}

	private static string Normalize(string currency) => currency.Trim().ToUpperInvariant();
}

public sealed class FxRateCacheEntry
{
	public FxRateCacheEntry(ExchangeRate rate, DateTimeOffset fetchedAt, DateTimeOffset expiresAt)
	{
		Rate = rate;
		FetchedAt = fetchedAt;
		ExpiresAt = expiresAt;
	}

	public ExchangeRate Rate { get; }
	public DateTimeOffset FetchedAt { get; }
	public DateTimeOffset ExpiresAt { get; }
}
