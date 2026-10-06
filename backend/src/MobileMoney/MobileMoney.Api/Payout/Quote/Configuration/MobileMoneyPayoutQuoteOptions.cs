namespace MobileMoney.Production.Payout.Quote.Configuration;

public sealed class MobileMoneyPayoutQuoteOptions
{
    public const string SectionName = "MobileMoney:Payout:Quote";

    public int LifetimeSeconds { get; init; } = 600;

    public Dictionary<string, byte> CurrencyMinorUnits { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    public List<MobileMoneyPayoutQuoteFeeTierOption> FeeTiers { get; init; } = [];
}

public sealed class MobileMoneyPayoutQuoteFeeTierOption
{
    public string SourceCountryCode { get; init; } = string.Empty;
    public string SourceCurrency { get; init; } = string.Empty;
    public string DestinationCountryCode { get; init; } = string.Empty;
    public string DestinationCurrency { get; init; } = string.Empty;
    public string OperatorCode { get; init; } = string.Empty;
    public long MinimumSourceAmountMinor { get; init; } = 1;
    public long? MaximumSourceAmountMinorExclusive { get; init; }
    public List<MobileMoneyPayoutQuoteFeeOption> Fees { get; init; } = [];
}

public sealed class MobileMoneyPayoutQuoteFeeOption
{
    public string Code { get; init; } = string.Empty;
    public long AmountMinor { get; init; }
    public string Currency { get; init; } = string.Empty;
}
