using MobileMoney.Production.Payout.Domain;

namespace MobileMoney.Production.Payout.Contracts;

public sealed record MobileMoneyPayoutResponse(
    Guid PayoutId,
    MobileMoneyPayoutStatus Status,
    string SourceWalletId,
    long AmountMinor,
    string Currency,
    MobileMoneyBeneficiaryResponse Beneficiary,
    string? ProviderReference,
    string? FailureCode,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
