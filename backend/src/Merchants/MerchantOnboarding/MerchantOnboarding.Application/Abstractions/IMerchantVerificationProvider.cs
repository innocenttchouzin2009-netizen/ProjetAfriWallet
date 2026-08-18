using AfriWallet.Merchants.Onboarding.Domain.Documents;

namespace AfriWallet.Merchants.Onboarding.Application.Abstractions;

public enum VerificationProviderDecision
{
    Verified = 0,
    Rejected = 1,
    ManualReviewRequired = 2
}

public sealed record MerchantVerificationProviderRequest(
    Guid VerificationId,
    string MerchantId,
    string OwnerAwid,
    string CountryCode,
    IReadOnlyCollection<VerificationDocument> Documents);

public sealed record MerchantVerificationProviderResult(
    VerificationProviderDecision Decision,
    string Reason,
    string ProviderReference);

public interface IMerchantVerificationProvider
{
    Task<MerchantVerificationProviderResult> VerifyAsync(MerchantVerificationProviderRequest request, CancellationToken cancellationToken = default);
}
