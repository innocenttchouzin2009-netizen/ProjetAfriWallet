namespace AfriWallet.Transfer.Domain.Funding;

public sealed record FundingAllocation
{
    public string SourceId { get; }
    public FundingSourceType SourceType { get; }
    public long AmountMinor { get; }
    public string CurrencyCode { get; }

    private FundingAllocation(
        string sourceId,
        FundingSourceType sourceType,
        long amountMinor,
        string currencyCode)
    {
        SourceId = sourceId;
        SourceType = sourceType;
        AmountMinor = amountMinor;
        CurrencyCode = currencyCode;
    }

    public static FundingAllocation Create(
        string sourceId,
        FundingSourceType sourceType,
        long amountMinor,
        string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException("Funding source id is required.", nameof(sourceId));
        }

        if (!Enum.IsDefined(typeof(FundingSourceType), sourceType))
        {
            throw new ArgumentOutOfRangeException(nameof(sourceType), "Funding source type is not supported.");
        }

        if (amountMinor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountMinor), "Funding amount must be positive.");
        }

        return new FundingAllocation(
            sourceId.Trim(),
            sourceType,
            amountMinor,
            NormalizeCurrency(currencyCode));
    }

    internal static string NormalizeCurrency(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Currency code is required.", nameof(currencyCode));
        }

        var normalized = currencyCode.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
        {
            throw new ArgumentException(
                "Currency code must contain exactly three ISO-like letters.",
                nameof(currencyCode));
        }

        return normalized;
    }
}
