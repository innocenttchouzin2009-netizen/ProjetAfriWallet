namespace AfriWallet.Disputes.Investigation.Domain.Evidence;

public sealed record DisputeEvidence(
    Guid EvidenceId,
    EvidenceType Type,
    string Reference,
    string Description,
    EvidenceStatus Status,
    EvidenceIntegrity Integrity,
    string SubmittedBy,
    DateTimeOffset SubmittedAtUtc);
