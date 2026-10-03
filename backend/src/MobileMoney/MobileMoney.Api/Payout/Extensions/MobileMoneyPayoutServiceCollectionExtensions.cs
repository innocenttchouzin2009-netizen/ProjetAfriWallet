using MobileMoney.Production.Payout.Abstractions;
using MobileMoney.Production.Payout.Application;
using MobileMoney.Production.Payout.Configuration;
using MobileMoney.Production.Payout.Domain;

namespace MobileMoney.Production.Payout.Extensions;

public static class MobileMoneyPayoutServiceCollectionExtensions
{
    public static IServiceCollection AddMobileMoneyPayoutEligibility(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration
            .GetSection(MobileMoneyPayoutOptions.SectionName)
            .Get<MobileMoneyPayoutOptions>() ?? new MobileMoneyPayoutOptions();

        var capabilities = options.Corridors.Select(option =>
            new MobileMoneyPayoutCorridorCapability(
                new MobileMoneyPayoutCorridor(
                    option.SourceCountryCode,
                    option.SourceCurrency,
                    option.DestinationCountryCode,
                    option.DestinationCurrency,
                    option.OperatorCode),
                option.Enabled,
                option.CollectionEnabled,
                option.OutboundPayoutEnabled))
            .ToArray();

        services.AddSingleton<IMobileMoneyPayoutEligibilityPolicy>(
            new ConfiguredMobileMoneyPayoutEligibilityPolicy(capabilities));

        return services;
    }
}
