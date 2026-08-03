using CurrencyEntity = global::UniversalWallet.Api.Domain.Currency.Currency;
using CurrencyStatus = global::UniversalWallet.Api.Domain.Currency.CurrencyStatus;

namespace UniversalWallet.Api.Infrastructure.Currency;

public interface ICurrencyRegistryRepository
{
	IReadOnlyList<CurrencyEntity> List();
	CurrencyEntity? GetByCode(string code);
	CurrencyEntity Add(CurrencyEntity currency);
	bool IsEnabled(string code);
	void SetStatus(string code, CurrencyStatus status);
}
