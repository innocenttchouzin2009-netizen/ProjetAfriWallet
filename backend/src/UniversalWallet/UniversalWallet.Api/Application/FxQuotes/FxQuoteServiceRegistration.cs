using UniversalWallet.Api.Infrastructure.FxQuotes;

namespace UniversalWallet.Api.Application.FxQuotes;

public static class FxQuoteServiceRegistration
{
	public static IServiceCollection AddFxQuotes(this IServiceCollection services)
	{
		services.AddSingleton<InMemoryQuoteRepository>();
		services.AddSingleton<QuoteCalculator>();
		services.AddSingleton<CreateQuoteHandler>();
		services.AddSingleton<GetQuoteHandler>();
		services.AddSingleton<AcceptQuoteHandler>();
		return services;
	}
}
