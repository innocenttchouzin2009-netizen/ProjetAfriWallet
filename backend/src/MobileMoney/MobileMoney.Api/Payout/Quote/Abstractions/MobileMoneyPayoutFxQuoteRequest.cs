using MobileMoney.Production.Payout.Domain;

namespace MobileMoney.Production.Payout.Quote.Abstractions;

public sealed record MobileMoneyPayoutFxQuoteRequest(
    MobileMoneyPayoutCorridor Corridor,
    long SourceAmountMinor);
