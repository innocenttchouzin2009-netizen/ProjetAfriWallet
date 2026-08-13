using AfriWallet.BankingPlatform.BankTransferIntent.Application.Interfaces;

namespace AfriWallet.BankingPlatform.BankTransferIntent.Infrastructure.Repositories;

public sealed class SandboxBeneficiaryRegistryGateway
    : IBeneficiaryRegistryGateway
{
    public Task<BeneficiaryAccountEligibility>
        GetEligibilityAsync(
            string ownerAwid,
            Guid beneficiaryId,
            Guid bankAccountId,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var exists =
            beneficiaryId != Guid.Empty &&
            bankAccountId != Guid.Empty;

        return Task.FromResult(
            new BeneficiaryAccountEligibility(
                Exists: exists,
                BeneficiaryActive: exists,
                BankAccountVerified: exists,
                CurrencyCode: "EUR"));
    }
}
