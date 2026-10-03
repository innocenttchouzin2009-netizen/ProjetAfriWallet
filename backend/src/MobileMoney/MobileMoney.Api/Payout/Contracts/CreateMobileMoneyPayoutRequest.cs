namespace MobileMoney.Production.Payout.Contracts;

public sealed record CreateMobileMoneyPayoutRequest(
    string SourceWalletId,
    long AmountMinor,
    string Currency,
    MobileMoneyBeneficiaryRequest Beneficiary,
    string IdempotencyKey);
