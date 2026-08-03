using UniversalWallet.Api.Domain.Currency;
using UniversalWallet.Api.Infrastructure.Currency;

namespace UniversalWallet.Api.Application.Fx;

public sealed class CurrencyRegistryService
{
	private readonly ICurrencyRegistryRepository _repository;

	public CurrencyRegistryService(ICurrencyRegistryRepository repository)
	{
		_repository = repository;
	}

	public IReadOnlyList<Currency> List() => _repository.List();

	public IReadOnlyList<Currency> ListActive() => _repository.List().Where(currency => currency.Status == CurrencyStatus.Active).ToList();

	public Currency GetRequired(string code)
	{
		var currency = _repository.GetByCode(code);
		if (currency is null)
		{
			throw new InvalidOperationException("CURRENCY_NOT_FOUND");
		}

		return currency;
	}

	public Currency RequireActive(string code)
	{
		var currency = GetRequired(code);
		if (currency.Status != CurrencyStatus.Active)
		{
			throw new InvalidOperationException(currency.Status == CurrencyStatus.Retired ? "CURRENCY_RETIRED" : "CURRENCY_DISABLED");
		}

		return currency;
	}

	public void EnsureEnabled(string code)
	{
		RequireActive(code);
	}
}
