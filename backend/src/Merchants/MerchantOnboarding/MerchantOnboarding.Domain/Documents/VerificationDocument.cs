namespace AfriWallet.Merchants.Onboarding.Domain.Documents;

public sealed record VerificationDocument(
    Guid DocumentId,
    VerificationDocumentType Type,
    string Reference,
    string Sha256,
    long SizeBytes,
    string ContentType,
    VerificationDocumentStatus Status,
    string SubmittedBy,
    DateTimeOffset SubmittedAtUtc);
