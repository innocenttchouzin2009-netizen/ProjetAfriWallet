using UniversalWallet.Api.Domain.Fx;

namespace UniversalWallet.Api.Infrastructure.Fx;

public sealed class StaticRateProvider : IFxRateProvider
{
	private readonly object _sync = new();
	private readonly Dictionary<(string BaseCurrency, string QuoteCurrency), decimal> _rates = new();

	public StaticRateProvider(string name = "StaticRateProvider")
	{
		Name = name;
		IsAvailable = true;
		SetRate("EUR", "XAF", 655.957m);
		SetRate("XAF", "EUR", 1m / 655.957m);
		SetRate("EUR", "USD", 1.09m);
		SetRate("USD", "EUR", 0.917431m);
		SetRate("USD", "XAF", 601.0m);
		SetRate("XAF", "USD", 1m / 601.0m);
	}

	public string Name { get; }
	public bool IsAvailable { get; set; }

	public void SetRate(string baseCurrency, string quoteCurrency, decimal rate)
	{
		lock (_sync)
		{
			_rates[(Normalize(baseCurrency), Normalize(quoteCurrency))] = rate;
		}
	}

	public Task<ExchangeRate> GetRateAsync(string baseCurrency, string quoteCurrency, CancellationToken cancellationToken)
	{
		if (!IsAvailable)
		{
			throw new InvalidOperationException("FX_PROVIDER_UNAVAILABLE");
		}

		lock (_sync)
		{
			var key = (Normalize(baseCurrency), Normalize(quoteCurrency));
			if (!_rates.TryGetValue(key, out var rate))
			{
				throw new InvalidOperationException("FX_RATE_NOT_FOUND");
			}

			var now = DateTimeOffset.UtcNow;
			return Task.FromResult(new ExchangeRate(
				Guid.CreateVersion7(),
				key.Item1,
				key.Item2,
				rate,
				Name,
				null,
				now,
				now.AddMinutes(10),
				ExchangeRateStatus.Active,
				1,
				now));
		}
	}

	public Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(string baseCurrency, CancellationToken cancellationToken)
	{
		var normalizedBaseCurrency = Normalize(baseCurrency);
		var rates = _rates.Keys
			.Where(key => key.BaseCurrency == normalizedBaseCurrency)
			.Select(key => new ExchangeRate(
				Guid.CreateVersion7(),
				key.BaseCurrency,
				key.QuoteCurrency,
				_rates[key],
				Name,
				null,
				DateTimeOffset.UtcNow,
				DateTimeOffset.UtcNow.AddMinutes(10),
				ExchangeRateStatus.Active,
				1,
				DateTimeOffset.UtcNow))
			.ToList();
		return Task.FromResult<IReadOnlyList<ExchangeRate>>(rates);
	}

	private static string Normalize(string currency) => currency.Trim().ToUpperInvariant();
}
