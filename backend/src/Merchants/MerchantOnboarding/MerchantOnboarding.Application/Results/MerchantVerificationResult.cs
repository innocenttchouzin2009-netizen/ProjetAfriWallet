using AfriWallet.Merchants.Onboarding.Domain.Cases;

namespace AfriWallet.Merchants.Onboarding.Application.Results;

public sealed record MerchantVerificationResult(
    Guid VerificationId,
    string MerchantId,
    string OwnerAwid,
    MerchantVerificationStatus Status,
    MerchantVerificationDecision Decision,
    string? AssignedReviewer,
    int DocumentCount,
    int NoteCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
