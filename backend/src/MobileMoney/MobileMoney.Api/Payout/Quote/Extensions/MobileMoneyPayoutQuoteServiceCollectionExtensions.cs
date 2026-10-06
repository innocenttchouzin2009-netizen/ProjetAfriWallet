using System.Globalization;
using AfriWallet.Fx.Application;
using AfriWallet.Fx.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MobileMoney.Production.Payout.Abstractions;
using MobileMoney.Production.Payout.Domain;
using MobileMoney.Production.Payout.Quote.Abstractions;
using MobileMoney.Production.Payout.Quote.Application;
using MobileMoney.Production.Payout.Quote.Configuration;
using MobileMoney.Production.Payout.Quote.Domain;
using MobileMoney.Production.Payout.Quote.Infrastructure;

namespace MobileMoney.Production.Payout.Quote.Extensions;

public static class MobileMoneyPayoutQuoteServiceCollectionExtensions
{
    public static IServiceCollection AddMobileMoneyPayoutQuote(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration
            .GetSection(MobileMoneyPayoutQuoteOptions.SectionName)
            .Get<MobileMoneyPayoutQuoteOptions>()
            ?? new MobileMoneyPayoutQuoteOptions();

        if (options.LifetimeSeconds <= 0)
        {
            throw new InvalidOperationException(
                "Mobile Money payout quote lifetime must be positive.");
        }

        var configuredFxRates = LoadFxRates(configuration);
        var configuredFeeTiers = BuildFeeTiers(options);

        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.TryAddSingleton<IFxQuoteProvider>(serviceProvider =>
            new ConfiguredFxQuoteProvider(
                configuredFxRates,
                serviceProvider.GetRequiredService<TimeProvider>()));
        services.TryAddSingleton<FxConversionService>();

        services.TryAddSingleton<IMobileMoneyPayoutCurrencyMinorUnitProvider>(
            new ConfiguredMobileMoneyPayoutCurrencyMinorUnitProvider(
                options.CurrencyMinorUnits));

        services.TryAddSingleton<IMobileMoneyPayoutFeePolicy>(
            new ConfiguredMobileMoneyPayoutFeePolicy(configuredFeeTiers));

        services.TryAddSingleton<
            IMobileMoneyPayoutFxQuoteProvider,
            CoreFxMobileMoneyPayoutFxQuoteProvider>();

        services.TryAddSingleton<
            IMobileMoneyPayoutClock,
            SystemMobileMoneyPayoutClock>();

        services.TryAddSingleton<MobileMoneyPayoutQuoteOrchestrator>(
            serviceProvider =>
                new MobileMoneyPayoutQuoteOrchestrator(
                    serviceProvider.GetRequiredService<IMobileMoneyPayoutEligibilityPolicy>(),
                    serviceProvider.GetRequiredService<IMobileMoneyPayoutFxQuoteProvider>(),
                    serviceProvider.GetRequiredService<IMobileMoneyPayoutFeePolicy>(),
                    serviceProvider.GetRequiredService<IMobileMoneyPayoutClock>(),
                    TimeSpan.FromSeconds(options.LifetimeSeconds)));

        return services;
    }

    private static IReadOnlyList<ConfiguredFxRate> LoadFxRates(
        IConfiguration configuration)
    {
        var rates = new List<ConfiguredFxRate>();

        foreach (var rateSection in configuration
                     .GetSection("Fx:Rates")
                     .GetChildren())
        {
            var sourceCurrencyCode = rateSection["SourceCurrencyCode"];
            var targetCurrencyCode = rateSection["TargetCurrencyCode"];
            var rateText = rateSection["Rate"];

            if (string.IsNullOrWhiteSpace(sourceCurrencyCode)
                || string.IsNullOrWhiteSpace(targetCurrencyCode)
                || !decimal.TryParse(
                    rateText,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var rate))
            {
                throw new InvalidOperationException(
                    "Each Fx:Rates entry must define SourceCurrencyCode, " +
                    "TargetCurrencyCode and a valid invariant decimal Rate.");
            }

            rates.Add(
                new ConfiguredFxRate(
                    sourceCurrencyCode,
                    targetCurrencyCode,
                    rate));
        }

        return rates;
    }

    private static IReadOnlyList<ConfiguredMobileMoneyPayoutFeeTier> BuildFeeTiers(
        MobileMoneyPayoutQuoteOptions options) =>
        options.FeeTiers
            .Select(tier =>
            {
                var corridor = new MobileMoneyPayoutCorridor(
                    tier.SourceCountryCode,
                    tier.SourceCurrency,
                    tier.DestinationCountryCode,
                    tier.DestinationCurrency,
                    tier.OperatorCode);

                var fees = tier.Fees.Select(fee =>
                    new MobileMoneyPayoutFee(
                        fee.Code,
                        fee.AmountMinor,
                        fee.Currency));

                return new ConfiguredMobileMoneyPayoutFeeTier(
                    corridor,
                    tier.MinimumSourceAmountMinor,
                    tier.MaximumSourceAmountMinorExclusive,
                    fees);
            })
            .ToArray();
}
