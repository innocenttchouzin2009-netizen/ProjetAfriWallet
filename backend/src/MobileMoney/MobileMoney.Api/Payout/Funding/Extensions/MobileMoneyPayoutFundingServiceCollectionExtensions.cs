using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MobileMoney.Production.Payout.Funding.Abstractions;
using MobileMoney.Production.Payout.Funding.Application;
using MobileMoney.Production.Payout.Funding.Infrastructure;
using MobileMoney.Production.Payout.Funding.Infrastructure.Persistence;

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

        var connectionString =
            configuration.GetConnectionString("MobileMoneyPayoutFunding")
            ?? "Data Source=mobile-money-payout-funding.db";

        services.AddDbContext<MobileMoneyPayoutFundingDbContext>(dbOptions =>
            dbOptions.UseSqlite(connectionString));

        services.TryAddScoped<
            IMobileMoneyPayoutFundingAttemptStore,
            EfMobileMoneyPayoutFundingAttemptStore>();

        services.TryAddSingleton<MobileMoneyPayoutSplitFundingValidator>();
        services.TryAddScoped<MobileMoneyPayoutFundingPlanningService>();

        return services;
    }
}
