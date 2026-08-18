using AfriWallet.Merchants.Onboarding.Domain.Documents;

namespace AfriWallet.Merchants.Onboarding.Api.Contracts;

public sealed record CreateVerificationRequest(string MerchantId);

public sealed record AddVerificationDocumentRequest(
    VerificationDocumentType Type,
    string Reference,
    string Sha256,
    long SizeBytes,
    string ContentType,
    string SubmittedBy);

public sealed record AssignReviewerRequest(string Reviewer);
public sealed record AddReviewNoteRequest(string Note);
