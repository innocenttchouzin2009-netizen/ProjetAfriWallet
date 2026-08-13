using AfriWallet.BankingPlatform.BeneficiaryRegistry.Domain.Beneficiaries;

namespace AfriWallet.BankingPlatform.BeneficiaryRegistry.Application.Interfaces;

public interface IBeneficiaryRepository
{
    Task AddAsync(
        BankBeneficiary beneficiary,
        CancellationToken cancellationToken);

    Task<BankBeneficiary?> GetAsync(
        Guid beneficiaryId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<BankBeneficiary>> ListByOwnerAsync(
        string ownerAwid,
        CancellationToken cancellationToken);

    Task<BankBeneficiary?> FindByBankIdentifierAsync(
        string ownerAwid,
        string normalizedIdentifier,
        CancellationToken cancellationToken);
}
