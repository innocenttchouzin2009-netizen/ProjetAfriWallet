namespace MobileMoney.Production.Payout.Contracts;

public sealed record CreateMobileMoneyPayoutRequest(
    string SourceWalletId,
    string SourceCountryCode,
    string SourceCurrency,
    long AmountMinor,
    string Currency,
    MobileMoneyBeneficiaryRequest Beneficiary,
    string IdempotencyKey);
