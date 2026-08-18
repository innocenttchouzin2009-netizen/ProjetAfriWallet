using AfriWallet.Merchants.Onboarding.Domain.Documents;

namespace AfriWallet.Merchants.Onboarding.Application.Policies;

/// Sandbox policy only; does not represent all KYB obligations of any jurisdiction.
public sealed class MerchantVerificationPolicy
{
    public bool HasMinimumDocuments(IReadOnlyCollection<VerificationDocument> documents)
    {
        var types = documents.Select(x => x.Type).ToHashSet();
        return types.Contains(VerificationDocumentType.BusinessRegistration) &&
               types.Contains(VerificationDocumentType.ProofOfAddress) &&
               types.Contains(VerificationDocumentType.OwnerIdentity);
    }
}
