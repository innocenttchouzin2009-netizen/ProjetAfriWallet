using MobileMoney.Production.Payout.Abstractions;
using MobileMoney.Production.Payout.Application;
using MobileMoney.Production.Payout.Configuration;
using MobileMoney.Production.Payout.Runtime;

namespace MobileMoney.Production.Payout.Extensions;

public static class MobileMoneyPayoutRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddMobileMoneyPayoutRuntimeFoundation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration
            .GetSection(MobileMoneyPayoutRuntimeOptions.SectionName)
            .Get<MobileMoneyPayoutRuntimeOptions>() ??
            new MobileMoneyPayoutRuntimeOptions();

        var storePath = Path.IsPathRooted(options.StorePath)
            ? options.StorePath
            : Path.Combine(AppContext.BaseDirectory, options.StorePath);

        services.AddSingleton<IMobileMoneyPayoutStore>(
            _ => new FileMobileMoneyPayoutStore(storePath));
        services.AddSingleton<IMobileMoneyPayoutClock, SystemMobileMoneyPayoutClock>();
        services.AddScoped<MobileMoneyPayoutOrchestrator>();

        return services;
    }
}
