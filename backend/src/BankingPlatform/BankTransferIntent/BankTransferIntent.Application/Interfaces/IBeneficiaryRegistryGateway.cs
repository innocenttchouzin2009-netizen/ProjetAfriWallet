namespace AfriWallet.BankingPlatform.BankTransferIntent.Application.Interfaces;

public interface IBeneficiaryRegistryGateway
{
    Task<BeneficiaryAccountEligibility> GetEligibilityAsync(
        string ownerAwid,
        Guid beneficiaryId,
        Guid bankAccountId,
        CancellationToken cancellationToken);
}

public sealed record BeneficiaryAccountEligibility(
    bool Exists,
    bool BeneficiaryActive,
    bool BankAccountVerified,
    string CurrencyCode);
