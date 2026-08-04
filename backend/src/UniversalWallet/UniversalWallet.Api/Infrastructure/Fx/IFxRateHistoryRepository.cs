using UniversalWallet.Api.Domain.Fx;

namespace UniversalWallet.Api.Infrastructure.Fx;

public interface IFxRateHistoryRepository
{
	void Save(ExchangeRate rate);
	IReadOnlyList<ExchangeRate> List();
	IReadOnlyList<ExchangeRate> List(string baseCurrency, string quoteCurrency);
}
