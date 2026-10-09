using Microsoft.Extensions.DependencyInjection.Extensions;
using MobileMoney.Production.Payout.Funding.Abstractions;
using MobileMoney.Production.Payout.Funding.Application;
using MobileMoney.Production.Payout.Funding.Infrastructure;

namespace MobileMoney.Production.Payout.Funding.Extensions;

public static class MobileMoneyPayoutFundingServiceCollectionExtensions
{
    public static IServiceCollection AddMobileMoneyPayoutFundingApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton<IMobileMoneyPayoutFundingSourceReader>(
            new ConfiguredMobileMoneyPayoutFundingSourceReader(configuration));

        services.TryAddSingleton<
            IMobileMoneyPayoutFundingAttemptStore,
            InMemoryMobileMoneyPayoutFundingAttemptStore>();

        services.TryAddSingleton<MobileMoneyPayoutSplitFundingValidator>();
        services.TryAddSingleton<MobileMoneyPayoutFundingPlanningService>();

        return services;
    }
}
