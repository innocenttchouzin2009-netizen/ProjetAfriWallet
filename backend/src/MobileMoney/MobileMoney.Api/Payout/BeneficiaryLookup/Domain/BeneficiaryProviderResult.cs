namespace MobileMoney.Production.Payout.BeneficiaryLookup.Domain;

public sealed record BeneficiaryProviderResult(
    BeneficiaryProviderStatus Status,
    string? AccountHolderName);
