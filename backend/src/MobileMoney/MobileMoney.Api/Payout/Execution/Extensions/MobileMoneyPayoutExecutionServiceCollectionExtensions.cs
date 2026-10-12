using Microsoft.Extensions.DependencyInjection.Extensions;
using MobileMoney.Production.Payout.Execution.Abstractions;
using MobileMoney.Production.Payout.Execution.Application;
using MobileMoney.Production.Payout.Execution.Infrastructure;

namespace MobileMoney.Production.Payout.Execution.Extensions;

public static class MobileMoneyPayoutExecutionServiceCollectionExtensions
{
    public static IServiceCollection AddMobileMoneyPayoutExecutionApi(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<
            IMobileMoneyPayoutExecutionQuoteBindingReader,
            StoreBackedMobileMoneyPayoutExecutionQuoteBindingReader>();

        services.TryAddScoped<
            IMobileMoneyPayoutExecutionFundingBindingReader,
            StoreBackedMobileMoneyPayoutExecutionFundingBindingReader>();

        services.TryAddSingleton<
            IMobileMoneyPayoutExecutionBindingStore,
            InMemoryMobileMoneyPayoutExecutionBindingStore>();

        services.TryAddScoped<MobileMoneyPayoutExecutionOrchestrator>();

        return services;
    }
}
