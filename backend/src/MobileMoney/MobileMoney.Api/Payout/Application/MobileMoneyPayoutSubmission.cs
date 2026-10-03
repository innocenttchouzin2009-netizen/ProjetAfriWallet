using MobileMoney.Production.Payout.Domain;

namespace MobileMoney.Production.Payout.Application;

public sealed record MobileMoneyPayoutSubmission(
    Guid PayoutId,
    string SourceWalletId,
    string SourceCountryCode,
    string SourceCurrency,
    long AmountMinor,
    string Currency,
    MobileMoneyBeneficiary Beneficiary,
    string IdempotencyKey);
