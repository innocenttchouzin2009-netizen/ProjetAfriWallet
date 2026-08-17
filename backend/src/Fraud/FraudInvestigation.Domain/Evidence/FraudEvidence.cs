namespace AfriWallet.Fraud.Investigation.Domain.Evidence;

public sealed record FraudEvidence(Guid EvidenceId, FraudEvidenceType Type, string ReferenceId, string Summary, DateTimeOffset LinkedAtUtc);