namespace MobileMoney.Production.Payout.Execution.Domain;

public sealed record MobileMoneyPayoutExecutionQuoteBinding
{
    private MobileMoneyPayoutExecutionQuoteBinding(
        Guid quoteId,
        string sourceCurrency,
        long sourceAmountMinor,
        long totalFeeMinor,
        long totalSourceDebitMinor,
        string destinationCurrency,
        long destinationAmountMinor,
        DateTimeOffset expiresAtUtc)
    {
        QuoteId = quoteId;
        SourceCurrency = sourceCurrency;
        SourceAmountMinor = sourceAmountMinor;
        TotalFeeMinor = totalFeeMinor;
        TotalSourceDebitMinor = totalSourceDebitMinor;
        DestinationCurrency = destinationCurrency;
        DestinationAmountMinor = destinationAmountMinor;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid QuoteId { get; }
    public string SourceCurrency { get; }
    public long SourceAmountMinor { get; }
    public long TotalFeeMinor { get; }
    public long TotalSourceDebitMinor { get; }
    public string DestinationCurrency { get; }
    public long DestinationAmountMinor { get; }
    public DateTimeOffset ExpiresAtUtc { get; }

    public static MobileMoneyPayoutExecutionQuoteBinding Create(
        Guid quoteId,
        string sourceCurrency,
        long sourceAmountMinor,
        long totalFeeMinor,
        long totalSourceDebitMinor,
        string destinationCurrency,
        long destinationAmountMinor,
        DateTimeOffset expiresAtUtc)
    {
        if (quoteId == Guid.Empty)
            throw new ArgumentException("Quote id cannot be empty.", nameof(quoteId));
        if (sourceAmountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(sourceAmountMinor));
        if (totalFeeMinor < 0)
            throw new ArgumentOutOfRangeException(nameof(totalFeeMinor));
        if (destinationAmountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(destinationAmountMinor));
        if (expiresAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Quote expiry must be UTC.", nameof(expiresAtUtc));

        var normalizedSourceCurrency = NormalizeCurrency(sourceCurrency, nameof(sourceCurrency));
        var normalizedDestinationCurrency =
            NormalizeCurrency(destinationCurrency, nameof(destinationCurrency));
        var expectedDebit = checked(sourceAmountMinor + totalFeeMinor);

        if (totalSourceDebitMinor != expectedDebit)
        {
            throw new ArgumentException(
                "Total source debit must equal source amount plus total fees.",
                nameof(totalSourceDebitMinor));
        }

        return new MobileMoneyPayoutExecutionQuoteBinding(
            quoteId,
            normalizedSourceCurrency,
            sourceAmountMinor,
            totalFeeMinor,
            totalSourceDebitMinor,
            normalizedDestinationCurrency,
            destinationAmountMinor,
            expiresAtUtc);
    }

    private static string NormalizeCurrency(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Currency is required.", parameterName);

        return value.Trim().ToUpperInvariant();
    }
}
