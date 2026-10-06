using MobileMoney.Production.Payout.Quote.Abstractions;
using MobileMoney.Production.Payout.Quote.Domain;

namespace MobileMoney.Production.Payout.Quote.Infrastructure;

public sealed class ConfiguredMobileMoneyPayoutFeePolicy
    : IMobileMoneyPayoutFeePolicy
{
    private readonly IReadOnlyList<ConfiguredMobileMoneyPayoutFeeTier> _tiers;

    public ConfiguredMobileMoneyPayoutFeePolicy(
        IEnumerable<ConfiguredMobileMoneyPayoutFeeTier> tiers)
    {
        ArgumentNullException.ThrowIfNull(tiers);

        var materializedTiers = tiers.ToArray();
        if (materializedTiers.Any(tier => tier is null))
            throw new ArgumentException("Fee tiers cannot contain null entries.", nameof(tiers));

        EnsureNoOverlappingTiers(materializedTiers);
        _tiers = Array.AsReadOnly(materializedTiers);
    }

    public ValueTask<IReadOnlyList<MobileMoneyPayoutFee>> CalculateAsync(
        MobileMoneyPayoutFeeContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Corridor is null)
            throw new ArgumentException("Fee context corridor is required.", nameof(context));
        if (context.SourceAmountMinor <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(context),
                "Source amount must be positive.");

        cancellationToken.ThrowIfCancellationRequested();

        var tier = _tiers.SingleOrDefault(candidate =>
            candidate.Matches(context.Corridor, context.SourceAmountMinor));

        if (tier is null)
        {
            throw new KeyNotFoundException(
                $"Payout fee policy is not configured for corridor " +
                $"'{context.Corridor.SourceCountryCode}:{context.Corridor.SourceCurrency}->" +
                $"{context.Corridor.DestinationCountryCode}:{context.Corridor.DestinationCurrency}/" +
                $"{context.Corridor.OperatorCode}' at source amount {context.SourceAmountMinor}.");
        }

        return ValueTask.FromResult(tier.Fees);
    }

    private static void EnsureNoOverlappingTiers(
        IReadOnlyList<ConfiguredMobileMoneyPayoutFeeTier> tiers)
    {
        for (var leftIndex = 0; leftIndex < tiers.Count; leftIndex++)
        {
            for (var rightIndex = leftIndex + 1; rightIndex < tiers.Count; rightIndex++)
            {
                var left = tiers[leftIndex];
                var right = tiers[rightIndex];

                if (left.Corridor != right.Corridor)
                    continue;

                if (Overlaps(left, right))
                {
                    throw new ArgumentException(
                        "Configured payout fee tiers cannot overlap for the same corridor.",
                        nameof(tiers));
                }
            }
        }
    }

    private static bool Overlaps(
        ConfiguredMobileMoneyPayoutFeeTier left,
        ConfiguredMobileMoneyPayoutFeeTier right)
    {
        var leftMaximum = left.MaximumSourceAmountMinorExclusive ?? long.MaxValue;
        var rightMaximum = right.MaximumSourceAmountMinorExclusive ?? long.MaxValue;

        return left.MinimumSourceAmountMinor < rightMaximum
            && right.MinimumSourceAmountMinor < leftMaximum;
    }
}
