using MobileMoney.Production.Payout.Domain;

namespace MobileMoney.Production.Payout.Quote.Domain;

public sealed class MobileMoneyPayoutQuote
{
    private MobileMoneyPayoutQuote(
        Guid quoteId,
        MobileMoneyPayoutCorridor corridor,
        long sourceAmountMinor,
        long destinationAmountMinor,
        decimal fxRate,
        IReadOnlyList<MobileMoneyPayoutFee> fees,
        long totalFeeMinor,
        long totalSourceDebitMinor,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        QuoteId = quoteId;
        Corridor = corridor;
        SourceAmountMinor = sourceAmountMinor;
        DestinationAmountMinor = destinationAmountMinor;
        FxRate = fxRate;
        Fees = fees;
        TotalFeeMinor = totalFeeMinor;
        TotalSourceDebitMinor = totalSourceDebitMinor;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid QuoteId { get; }
    public MobileMoneyPayoutCorridor Corridor { get; }
    public long SourceAmountMinor { get; }
    public long DestinationAmountMinor { get; }
    public decimal FxRate { get; }
    public IReadOnlyList<MobileMoneyPayoutFee> Fees { get; }
    public long TotalFeeMinor { get; }
    public long TotalSourceDebitMinor { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; }

    public static MobileMoneyPayoutQuote Create(
        MobileMoneyPayoutCorridor corridor,
        long sourceAmountMinor,
        long destinationAmountMinor,
        decimal fxRate,
        IEnumerable<MobileMoneyPayoutFee> fees,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (corridor is null)
            throw new ArgumentNullException(nameof(corridor));
        if (sourceAmountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(sourceAmountMinor));
        if (destinationAmountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(destinationAmountMinor));
        if (fxRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(fxRate));
        if (fees is null)
            throw new ArgumentNullException(nameof(fees));

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureUtc(expiresAtUtc, nameof(expiresAtUtc));
        if (expiresAtUtc <= createdAtUtc)
            throw new ArgumentException(
                "Quote expiry must be after creation.",
                nameof(expiresAtUtc));

        var materializedFees = fees.ToArray();
        if (materializedFees.Any(fee => fee is null))
            throw new ArgumentException("Fees cannot contain null entries.", nameof(fees));
        if (materializedFees.Any(
                fee => !string.Equals(
                    fee.Currency,
                    corridor.SourceCurrency,
                    StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "All payout quote fees must use the source currency.",
                nameof(fees));
        }

        var totalFeeMinor = materializedFees.Sum(fee => fee.AmountMinor);
        var totalSourceDebitMinor = checked(sourceAmountMinor + totalFeeMinor);

        return new MobileMoneyPayoutQuote(
            Guid.NewGuid(),
            corridor,
            sourceAmountMinor,
            destinationAmountMinor,
            fxRate,
            Array.AsReadOnly(materializedFees),
            totalFeeMinor,
            totalSourceDebitMinor,
            createdAtUtc,
            expiresAtUtc);
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
    }
}
