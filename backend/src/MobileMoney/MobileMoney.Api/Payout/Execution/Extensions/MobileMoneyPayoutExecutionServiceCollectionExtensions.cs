using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MobileMoney.Production.Payout.Execution.Abstractions;
using MobileMoney.Production.Payout.Execution.Application;
using MobileMoney.Production.Payout.Execution.Infrastructure;
using MobileMoney.Production.Payout.Execution.Infrastructure.Persistence;

namespace MobileMoney.Production.Payout.Execution.Extensions;

public static class MobileMoneyPayoutExecutionServiceCollectionExtensions
{
    public static IServiceCollection AddMobileMoneyPayoutExecutionApi(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<
            IMobileMoneyPayoutExecutionQuoteBindingReader,
            StoreBackedMobileMoneyPayoutExecutionQuoteBindingReader>();

        services.TryAddScoped<
            IMobileMoneyPayoutExecutionFundingBindingReader,
            StoreBackedMobileMoneyPayoutExecutionFundingBindingReader>();

        var connectionString =
            configuration?.GetConnectionString("MobileMoneyPayoutExecution")
            ?? "Data Source=mobile-money-payout-execution.db";

        services.AddDbContext<MobileMoneyPayoutExecutionDbContext>(dbOptions =>
            dbOptions.UseSqlite(connectionString));

        services.TryAddScoped<
            IMobileMoneyPayoutExecutionBindingStore,
            EfMobileMoneyPayoutExecutionBindingStore>();

        services.TryAddScoped<MobileMoneyPayoutExecutionOrchestrator>();

        return services;
    }
}
