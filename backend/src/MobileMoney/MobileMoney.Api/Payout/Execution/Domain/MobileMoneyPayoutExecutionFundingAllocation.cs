using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Execution.Domain;

public sealed record MobileMoneyPayoutExecutionFundingAllocation
{
    private MobileMoneyPayoutExecutionFundingAllocation(
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

    public string SourceId { get; }
    public FundingSourceType SourceType { get; }
    public long AmountMinor { get; }
    public string CurrencyCode { get; }

    public static MobileMoneyPayoutExecutionFundingAllocation Create(
        string sourceId,
        FundingSourceType sourceType,
        long amountMinor,
        string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            throw new ArgumentException("Funding source id is required.", nameof(sourceId));
        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor));
        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("Funding currency is required.", nameof(currencyCode));

        return new MobileMoneyPayoutExecutionFundingAllocation(
            sourceId.Trim(),
            sourceType,
            amountMinor,
            currencyCode.Trim().ToUpperInvariant());
    }
}
