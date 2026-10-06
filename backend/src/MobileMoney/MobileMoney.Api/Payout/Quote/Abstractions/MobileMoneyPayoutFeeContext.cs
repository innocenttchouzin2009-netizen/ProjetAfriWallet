using MobileMoney.Production.Payout.Domain;

namespace MobileMoney.Production.Payout.Quote.Abstractions;

public sealed record MobileMoneyPayoutFeeContext(
    MobileMoneyPayoutCorridor Corridor,
    long SourceAmountMinor);
