using MobileMoney.Production.Payout.Domain;

namespace MobileMoney.Production.Payout.Runtime;

internal sealed record MobileMoneyPayoutSnapshot(
    Guid PayoutId,
    string SourceWalletId,
    long AmountMinor,
    string Currency,
    string BeneficiaryMsisdn,
    string BeneficiaryCountryCode,
    string BeneficiaryOperatorCode,
    string? BeneficiaryDisplayName,
    string IdempotencyKey,
    MobileMoneyPayoutStatus Status,
    string? ProviderReference,
    string? FailureCode,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static MobileMoneyPayoutSnapshot FromDomain(MobileMoneyPayout payout) =>
        new(
            payout.PayoutId,
            payout.SourceWalletId,
            payout.AmountMinor,
            payout.Currency,
            payout.Beneficiary.Msisdn,
            payout.Beneficiary.CountryCode,
            payout.Beneficiary.OperatorCode,
            payout.Beneficiary.DisplayName,
            payout.IdempotencyKey,
            payout.Status,
            payout.ProviderReference,
            payout.FailureCode,
            payout.CreatedAtUtc,
            payout.UpdatedAtUtc);

    public MobileMoneyPayout ToDomain() =>
        MobileMoneyPayout.Restore(
            PayoutId,
            SourceWalletId,
            AmountMinor,
            Currency,
            new MobileMoneyBeneficiary(
                BeneficiaryMsisdn,
                BeneficiaryCountryCode,
                BeneficiaryOperatorCode,
                BeneficiaryDisplayName),
            IdempotencyKey,
            Status,
            ProviderReference,
            FailureCode,
            CreatedAtUtc,
            UpdatedAtUtc);
}
