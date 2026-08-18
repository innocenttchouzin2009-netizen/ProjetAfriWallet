using AfriWallet.Merchants.Onboarding.Domain.Documents;

namespace AfriWallet.Merchants.Onboarding.Application.Commands;

public sealed record CreateVerificationCommand(string MerchantId, string Actor);

public sealed record AddVerificationDocumentCommand(
    Guid VerificationId,
    VerificationDocumentType Type,
    string Reference,
    string Sha256,
    long SizeBytes,
    string ContentType,
    string SubmittedBy,
    string Actor);

public sealed record AssignVerificationReviewerCommand(Guid VerificationId, string Reviewer, string Actor);
public sealed record AddVerificationNoteCommand(Guid VerificationId, string Note, string Actor);
public sealed record ExecuteVerificationCommand(Guid VerificationId, string Actor);
