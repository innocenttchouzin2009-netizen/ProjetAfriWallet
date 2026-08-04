namespace UniversalWallet.Api.Payments.Domain.Limits;

public enum PaymentLimitScope
{
    User,
    Wallet,
    Country,
    Channel,
    RecipientType
}

public sealed class PaymentLimits
{
    public long? PerTransactionMinor { get; init; }
    public long? DailyMinor { get; init; }
    public long? MonthlyMinor { get; init; }
    public int? DailyCount { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public PaymentLimitScope Scope { get; init; }
}
