using BankRouting.Application.Interfaces;
using BankRouting.Domain.Rails;

namespace BankRouting.Infrastructure.Registries;

public sealed class InMemoryBankRailRegistry : IBankRailRegistry
{
    private readonly IReadOnlyCollection<BankRail> _rails =
    [
        new BankRail(
            RailId: "sepa-de-eur",
            RailType: BankRailType.Sepa,
            CountryCode: "DE",
            CurrencyCode: "EUR",
            IsActive: true,
            IsHealthy: true,
            MinAmountMinor: 1,
            MaxAmountMinor: 500000,
            Priority: 2,
            EstimatedCostMinor: 150,
            Description: "Standard EU bank transfer"),
        new BankRail(
            RailId: "sepa-instant-de-eur",
            RailType: BankRailType.SepaInstant,
            CountryCode: "DE",
            CurrencyCode: "EUR",
            IsActive: true,
            IsHealthy: true,
            MinAmountMinor: 1,
            MaxAmountMinor: 200000,
            Priority: 1,
            EstimatedCostMinor: 220,
            Description: "Preferred EU instant settlement rail"),
        new BankRail(
            RailId: "swift-fr-eur",
            RailType: BankRailType.Swift,
            CountryCode: "FR",
            CurrencyCode: "EUR",
            IsActive: true,
            IsHealthy: true,
            MinAmountMinor: 50,
            MaxAmountMinor: 2000000,
            Priority: 3,
            EstimatedCostMinor: 400,
            Description: "International transfer rail"),
        new BankRail(
            RailId: "local-ng-ngn",
            RailType: BankRailType.LocalBankTransfer,
            CountryCode: "NG",
            CurrencyCode: "NGN",
            IsActive: true,
            IsHealthy: true,
            MinAmountMinor: 100,
            MaxAmountMinor: 10000000,
            Priority: 1,
            EstimatedCostMinor: 80,
            Description: "Domestic market rail")
    ];

    public Task<IReadOnlyCollection<BankRail>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_rails);
    }
}
