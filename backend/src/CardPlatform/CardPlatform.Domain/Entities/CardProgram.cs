namespace AfriWallet.CardPlatform.Domain.Entities;

public sealed class CardProgram
{
    public string ProgramId { get; set; } = Guid.NewGuid().ToString("N");
    public string ProgramCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Network { get; set; } = "Visa";
    public string CardType { get; set; } = "virtual";
    public string FundingType { get; set; } = "prepaid";
    public string CountryCode { get; set; } = "CM";
    public string BaseCurrency { get; set; } = "XAF";
    public List<string> SupportedCurrencies { get; set; } = [];
    public string Environment { get; set; } = "Sandbox";
    public string Status { get; set; } = "Active";
    public CardProgramCapabilities Capabilities { get; set; } = new();
    public CardProgramLimits Limits { get; set; } = new();
    public CardProgramFees Fees { get; set; } = new();
    public int Priority { get; set; } = 100;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int Version { get; set; } = 1;
}

public sealed class CardProgramCapabilities
{
    public bool OnlinePayments { get; set; } = true;
    public bool InStorePayments { get; set; } = true;
    public bool AtmWithdrawals { get; set; } = false;
    public bool ContactlessPayments { get; set; } = true;
    public bool InternationalPayments { get; set; } = true;
    public bool RecurringPayments { get; set; } = false;
}

public sealed class CardProgramLimits
{
    public long SingleTransactionLimitMinor { get; set; } = 500_000;
    public long DailyLimitMinor { get; set; } = 2_000_000;
    public long MonthlyLimitMinor { get; set; } = 10_000_000;
}

public sealed class CardProgramFees
{
    public long CardIssueFeeMinor { get; set; } = 0;
    public long AnnualFeeMinor { get; set; } = 0;
    public decimal ForeignExchangeMarkupPercent { get; set; } = 0.5m;
}
