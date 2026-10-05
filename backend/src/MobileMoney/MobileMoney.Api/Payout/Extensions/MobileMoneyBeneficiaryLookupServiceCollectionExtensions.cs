using Microsoft.Extensions.DependencyInjection.Extensions;
using MobileMoney.Production.Payout.BeneficiaryLookup.Application;
using MobileMoney.Production.Payout.BeneficiaryLookup.Application.Abstractions;

namespace MobileMoney.Production.Payout.Extensions;

public static class MobileMoneyBeneficiaryLookupServiceCollectionExtensions
{
    public static IServiceCollection AddMobileMoneyBeneficiaryLookup(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<CameroonOperatorResolver>();
        services.TryAddSingleton<
            IBeneficiaryAccountHolderResolver,
            UnavailableBeneficiaryAccountHolderResolver>();
        services.AddSingleton<BeneficiaryAccountHolderLookupService>();

        return services;
    }
}
