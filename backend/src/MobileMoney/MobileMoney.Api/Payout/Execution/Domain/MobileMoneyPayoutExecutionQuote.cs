using MobileMoney.Production.Payout.Domain;
using MobileMoney.Production.Payout.Quote.Domain;

namespace MobileMoney.Production.Payout.Execution.Domain;

public sealed record MobileMoneyPayoutExecutionQuote
{
    public MobileMoneyPayoutExecutionQuote(
        Guid quoteId,
        MobileMoneyPayoutCorridor corridor,
        long sourceAmountMinor,
        long destinationAmountMinor,
        decimal fxRate,
        long totalFeeMinor,
        long totalSourceDebitMinor,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (quoteId == Guid.Empty)
            throw new ArgumentException("Quote id is required.", nameof(quoteId));
        Corridor = corridor ?? throw new ArgumentNullException(nameof(corridor));
        if (sourceAmountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(sourceAmountMinor));
        if (destinationAmountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(destinationAmountMinor));
        if (fxRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(fxRate));
        if (totalFeeMinor < 0)
            throw new ArgumentOutOfRangeException(nameof(totalFeeMinor));
        if (totalSourceDebitMinor != checked(sourceAmountMinor + totalFeeMinor))
            throw new ArgumentException(
                "Total source debit must equal source amount plus fees.",
                nameof(totalSourceDebitMinor));
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureUtc(expiresAtUtc, nameof(expiresAtUtc));
        if (expiresAtUtc <= createdAtUtc)
            throw new ArgumentException(
                "Quote expiry must be after creation.",
                nameof(expiresAtUtc));

        QuoteId = quoteId;
        SourceAmountMinor = sourceAmountMinor;
        DestinationAmountMinor = destinationAmountMinor;
        FxRate = fxRate;
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
    public long TotalFeeMinor { get; }
    public long TotalSourceDebitMinor { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; }

    public static MobileMoneyPayoutExecutionQuote FromQuote(
        MobileMoneyPayoutQuote quote)
    {
        ArgumentNullException.ThrowIfNull(quote);

        return new(
            quote.QuoteId,
            quote.Corridor,
            quote.SourceAmountMinor,
            quote.DestinationAmountMinor,
            quote.FxRate,
            quote.TotalFeeMinor,
            quote.TotalSourceDebitMinor,
            quote.CreatedAtUtc,
            quote.ExpiresAtUtc);
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
    }
}
