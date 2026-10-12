namespace MobileMoney.Production.Payout.Execution.Domain;

public sealed record MobileMoneyPayoutExecutionQuoteBinding
{
    private MobileMoneyPayoutExecutionQuoteBinding(
        Guid quoteId,
        string sourceCountryCode,
        string sourceCurrency,
        long sourceAmountMinor,
        long totalFeeMinor,
        long totalSourceDebitMinor,
        string destinationCountryCode,
        string destinationCurrency,
        long destinationAmountMinor,
        string operatorCode,
        DateTimeOffset expiresAtUtc)
    {
        QuoteId = quoteId;
        SourceCountryCode = sourceCountryCode;
        SourceCurrency = sourceCurrency;
        SourceAmountMinor = sourceAmountMinor;
        TotalFeeMinor = totalFeeMinor;
        TotalSourceDebitMinor = totalSourceDebitMinor;
        DestinationCountryCode = destinationCountryCode;
        DestinationCurrency = destinationCurrency;
        DestinationAmountMinor = destinationAmountMinor;
        OperatorCode = operatorCode;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid QuoteId { get; }
    public string SourceCountryCode { get; }
    public string SourceCurrency { get; }
    public long SourceAmountMinor { get; }
    public long TotalFeeMinor { get; }
    public long TotalSourceDebitMinor { get; }
    public string DestinationCountryCode { get; }
    public string DestinationCurrency { get; }
    public long DestinationAmountMinor { get; }
    public string OperatorCode { get; }
    public DateTimeOffset ExpiresAtUtc { get; }

    public static MobileMoneyPayoutExecutionQuoteBinding Create(
        Guid quoteId,
        string sourceCountryCode,
        string sourceCurrency,
        long sourceAmountMinor,
        long totalFeeMinor,
        long totalSourceDebitMinor,
        string destinationCountryCode,
        string destinationCurrency,
        long destinationAmountMinor,
        string operatorCode,
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

        var normalizedSourceCountryCode =
            NormalizeCountryCode(sourceCountryCode, nameof(sourceCountryCode));
        var normalizedSourceCurrency =
            NormalizeCurrency(sourceCurrency, nameof(sourceCurrency));
        var normalizedDestinationCountryCode =
            NormalizeCountryCode(destinationCountryCode, nameof(destinationCountryCode));
        var normalizedDestinationCurrency =
            NormalizeCurrency(destinationCurrency, nameof(destinationCurrency));
        var normalizedOperatorCode = NormalizeOperatorCode(operatorCode);
        var expectedDebit = checked(sourceAmountMinor + totalFeeMinor);

        if (totalSourceDebitMinor != expectedDebit)
        {
            throw new ArgumentException(
                "Total source debit must equal source amount plus total fees.",
                nameof(totalSourceDebitMinor));
        }

        return new MobileMoneyPayoutExecutionQuoteBinding(
            quoteId,
            normalizedSourceCountryCode,
            normalizedSourceCurrency,
            sourceAmountMinor,
            totalFeeMinor,
            totalSourceDebitMinor,
            normalizedDestinationCountryCode,
            normalizedDestinationCurrency,
            destinationAmountMinor,
            normalizedOperatorCode,
            expiresAtUtc);
    }

    private static string NormalizeCountryCode(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Country code is required.", parameterName);

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 2 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw new ArgumentException(
                "Country code must contain two ISO-like letters.",
                parameterName);

        return normalized;
    }

    private static string NormalizeCurrency(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Currency is required.", parameterName);

        return value.Trim().ToUpperInvariant();
    }

    private static string NormalizeOperatorCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Mobile money operator code is required.", nameof(value));

        return value.Trim().ToUpperInvariant();
    }
}
