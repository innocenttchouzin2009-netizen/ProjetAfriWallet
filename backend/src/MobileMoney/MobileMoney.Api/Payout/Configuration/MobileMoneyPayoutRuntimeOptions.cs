namespace MobileMoney.Production.Payout.Configuration;

public sealed class MobileMoneyPayoutRuntimeOptions
{
    public const string SectionName = "MobileMoney:Payout:Runtime";

    public string StorePath { get; init; } = "data/mobile-money-payouts.json";
}
