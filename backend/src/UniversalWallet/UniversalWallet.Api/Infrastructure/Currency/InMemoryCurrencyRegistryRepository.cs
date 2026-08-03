using CurrencyEntity = global::UniversalWallet.Api.Domain.Currency.Currency;
using UniversalWallet.Api.Domain.Currency;
using CurrencyStatus = global::UniversalWallet.Api.Domain.Currency.CurrencyStatus;

namespace UniversalWallet.Api.Infrastructure.Currency;

public sealed class InMemoryCurrencyRegistryRepository : ICurrencyRegistryRepository
{
	private readonly object _sync = new();
	private readonly Dictionary<string, CurrencyEntity> _currencies = new(StringComparer.OrdinalIgnoreCase);

	public InMemoryCurrencyRegistryRepository(bool seedDefaults = true)
	{
		if (!seedDefaults)
		{
			return;
		}

		Seed(new CurrencyEntity("EUR", 978, "Euro", 2, "€", "Europe", CurrencyStatus.Active, DateTimeOffset.UtcNow));
		Seed(new CurrencyEntity("USD", 840, "US Dollar", 2, "$", "North America", CurrencyStatus.Active, DateTimeOffset.UtcNow));
		Seed(new CurrencyEntity("XAF", 950, "Central African CFA Franc", 0, "FCFA", "Africa", CurrencyStatus.Active, DateTimeOffset.UtcNow));
		Seed(new CurrencyEntity("XOF", 952, "West African CFA Franc", 0, "FCFA", "Africa", CurrencyStatus.Active, DateTimeOffset.UtcNow));
		Seed(new CurrencyEntity("GBP", 826, "Pound Sterling", 2, "£", "Europe", CurrencyStatus.Active, DateTimeOffset.UtcNow));
		Seed(new CurrencyEntity("CAD", 124, "Canadian Dollar", 2, "CA$", "North America", CurrencyStatus.Active, DateTimeOffset.UtcNow));
		Seed(new CurrencyEntity("CHF", 756, "Swiss Franc", 2, "CHF", "Europe", CurrencyStatus.Active, DateTimeOffset.UtcNow));
		Seed(new CurrencyEntity("NGN", 566, "Nigerian Naira", 2, "₦", "Africa", CurrencyStatus.Active, DateTimeOffset.UtcNow));
		Seed(new CurrencyEntity("GHS", 936, "Ghanaian Cedi", 2, "GH₵", "Africa", CurrencyStatus.Active, DateTimeOffset.UtcNow));
		Seed(new CurrencyEntity("KES", 404, "Kenyan Shilling", 2, "KSh", "Africa", CurrencyStatus.Active, DateTimeOffset.UtcNow));
	}

	public IReadOnlyList<CurrencyEntity> List()
	{
		lock (_sync)
		{
			return _currencies.Values.OrderBy(currency => currency.Code).ToList();
		}
	}

	public CurrencyEntity? GetByCode(string code)
	{
		lock (_sync)
		{
			return _currencies.GetValueOrDefault(Normalize(code));
		}
	}

	public CurrencyEntity Add(CurrencyEntity currency)
	{
		lock (_sync)
		{
			var normalizedCode = Normalize(currency.Code);
			if (_currencies.ContainsKey(normalizedCode))
			{
				throw new InvalidOperationException("CURRENCY_DUPLICATE");
			}

			var normalizedCurrency = new CurrencyEntity(normalizedCode, currency.NumericCode, currency.Name, currency.MinorUnits, currency.Symbol, currency.Region, currency.Status, currency.CreatedAt);
			_currencies[normalizedCode] = normalizedCurrency;
			return normalizedCurrency;
		}
	}

	public bool IsEnabled(string code)
	{
		var currency = GetByCode(code);
		return currency is not null && currency.Status == CurrencyStatus.Active;
	}

	public void SetStatus(string code, CurrencyStatus status)
	{
		lock (_sync)
		{
			var normalized = Normalize(code);
			if (!_currencies.TryGetValue(normalized, out var currency))
			{
				throw new InvalidOperationException("CURRENCY_NOT_FOUND");
			}

			if (currency.Status == CurrencyStatus.Retired && status == CurrencyStatus.Active)
			{
				throw new InvalidOperationException("CURRENCY_RETIRED");
			}

			_currencies[normalized] = new CurrencyEntity(currency.Code, currency.NumericCode, currency.Name, currency.MinorUnits, currency.Symbol, currency.Region, status, currency.CreatedAt);
		}
	}

	private void Seed(CurrencyEntity currency)
	{
		_currencies[Normalize(currency.Code)] = currency;
	}

	private static string Normalize(string code)
	{
		return code.Trim().ToUpperInvariant();
	}
}
