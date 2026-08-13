using PaymentRouting.Application.Interfaces;
using PaymentRouting.Domain.Providers;
using PaymentRouting.Domain.Routes;

namespace PaymentRouting.Infrastructure.Providers;

public static class SandboxProviderBootstrap
{
    public static async Task SeedAsync(
        IPaymentProviderRepository repository,
        CancellationToken cancellationToken)
    {
        var providers = new[]
        {
            new PaymentProvider(
                "AFW-WALLET",
                "AfriWallet Internal Rail",
                PaymentRail.Wallet,
                ["CM", "DE", "FR", "BE"],
                ["XAF", "EUR"],
                baseCostScore: 1,
                priority: 1),

            new PaymentProvider(
                "MTN-MOMO-CM",
                "MTN Mobile Money Cameroon",
                PaymentRail.MobileMoney,
                ["CM"],
                ["XAF"],
                baseCostScore: 20,
                priority: 10),

            new PaymentProvider(
                "ORANGE-MONEY-CM",
                "Orange Money Cameroon",
                PaymentRail.MobileMoney,
                ["CM"],
                ["XAF"],
                baseCostScore: 18,
                priority: 8),

            new PaymentProvider(
                "BANK-SEPA",
                "SEPA Banking Rail",
                PaymentRail.Bank,
                ["DE", "FR", "BE"],
                ["EUR"],
                baseCostScore: 8,
                priority: 5),

            new PaymentProvider(
                "CARD-SANDBOX",
                "Card Processing Sandbox",
                PaymentRail.Card,
                ["CM", "DE", "FR", "BE"],
                ["XAF", "EUR"],
                baseCostScore: 25,
                priority: 15)
        };

        foreach (var provider in providers)
        {
            await repository.AddAsync(
                provider,
                cancellationToken);
        }
    }
}
