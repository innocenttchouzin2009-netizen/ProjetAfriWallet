using MobileMoney.Production.Payout.Domain;
using MobileMoney.Production.Payout.Quote.Domain;

namespace MobileMoney.Production.Payout.Quote.Infrastructure;

public sealed class ConfiguredMobileMoneyPayoutFeeTier
{
    public ConfiguredMobileMoneyPayoutFeeTier(
        MobileMoneyPayoutCorridor corridor,
        long minimumSourceAmountMinor,
        long? maximumSourceAmountMinorExclusive,
        IEnumerable<MobileMoneyPayoutFee> fees)
    {
        Corridor = corridor ?? throw new ArgumentNullException(nameof(corridor));

        if (minimumSourceAmountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(minimumSourceAmountMinor));

        if (maximumSourceAmountMinorExclusive is <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumSourceAmountMinorExclusive));

        if (maximumSourceAmountMinorExclusive.HasValue
            && maximumSourceAmountMinorExclusive.Value <= minimumSourceAmountMinor)
        {
            throw new ArgumentException(
                "Maximum source amount must be greater than the minimum source amount.",
                nameof(maximumSourceAmountMinorExclusive));
        }

        ArgumentNullException.ThrowIfNull(fees);

        var materializedFees = fees.ToArray();
        if (materializedFees.Any(fee => fee is null))
            throw new ArgumentException("Fees cannot contain null entries.", nameof(fees));

        if (materializedFees.Any(fee => !string.Equals(
                fee.Currency,
                corridor.SourceCurrency,
                StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "Configured payout fees must use the corridor source currency.",
                nameof(fees));
        }

        var duplicateCode = materializedFees
            .GroupBy(fee => fee.Code, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateCode is not null)
        {
            throw new ArgumentException(
                $"Fee code '{duplicateCode.Key}' is configured more than once in the same tier.",
                nameof(fees));
        }

        MinimumSourceAmountMinor = minimumSourceAmountMinor;
        MaximumSourceAmountMinorExclusive = maximumSourceAmountMinorExclusive;
        Fees = Array.AsReadOnly(materializedFees);
    }

    public MobileMoneyPayoutCorridor Corridor { get; }
    public long MinimumSourceAmountMinor { get; }
    public long? MaximumSourceAmountMinorExclusive { get; }
    public IReadOnlyList<MobileMoneyPayoutFee> Fees { get; }

    public bool Matches(
        MobileMoneyPayoutCorridor corridor,
        long sourceAmountMinor)
    {
        ArgumentNullException.ThrowIfNull(corridor);

        return Corridor == corridor
            && sourceAmountMinor >= MinimumSourceAmountMinor
            && (!MaximumSourceAmountMinorExclusive.HasValue
                || sourceAmountMinor < MaximumSourceAmountMinorExclusive.Value);
    }
}
