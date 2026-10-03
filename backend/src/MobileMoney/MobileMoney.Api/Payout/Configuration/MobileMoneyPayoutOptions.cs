namespace MobileMoney.Production.Payout.Configuration;

public sealed class MobileMoneyPayoutOptions
{
    public const string SectionName = "MobileMoney:Payout";

    public List<MobileMoneyPayoutCorridorOption> Corridors { get; init; } = [];
}

public sealed class MobileMoneyPayoutCorridorOption
{
    public string SourceCountryCode { get; init; } = string.Empty;
    public string SourceCurrency { get; init; } = string.Empty;
    public string DestinationCountryCode { get; init; } = string.Empty;
    public string DestinationCurrency { get; init; } = string.Empty;
    public string OperatorCode { get; init; } = string.Empty;
    public bool Enabled { get; init; }
    public bool CollectionEnabled { get; init; }
    public bool OutboundPayoutEnabled { get; init; }
}
