namespace MobileMoney.Production.Payout.Quote.Contracts;

public sealed record MobileMoneyPayoutQuoteFeeResponse(
    string Code,
    long AmountMinor,
    string Currency);
