using UniversalWallet.Api.Infrastructure.Currency;
using UniversalWallet.Api.Infrastructure.Fx;

namespace UniversalWallet.Api.Application.Fx;

public static class FxServiceRegistration
{
	public static IServiceCollection AddFxEngine(this IServiceCollection services)
	{
		services.AddSingleton<ICurrencyRegistryRepository, InMemoryCurrencyRegistryRepository>();
		services.AddSingleton<CurrencyRegistryService>();
		services.AddSingleton<IFxRateProvider, StaticRateProvider>();
		services.AddSingleton(new FxRateCache(TimeSpan.FromMinutes(5)));
		services.AddSingleton<IFxRateHistoryRepository, InMemoryFxRateHistoryRepository>();
		services.AddSingleton<IFxConversionRepository, InMemoryFxConversionRepository>();
		services.AddSingleton<FxEngineService>();
		services.AddSingleton<ConvertCurrencyHandler>();
		services.AddSingleton<GetExchangeRateHandler>();
		services.AddSingleton<UpdateExchangeRateHandler>();
		return services;
	}
}
