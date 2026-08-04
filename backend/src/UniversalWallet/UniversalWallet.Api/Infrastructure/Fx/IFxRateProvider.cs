using UniversalWallet.Api.Domain.Fx;

namespace UniversalWallet.Api.Infrastructure.Fx;

public interface IFxRateProvider
{
	string Name { get; }
	bool IsAvailable { get; }
	Task<ExchangeRate> GetRateAsync(string baseCurrency, string quoteCurrency, CancellationToken cancellationToken);
	Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(string baseCurrency, CancellationToken cancellationToken);
}
