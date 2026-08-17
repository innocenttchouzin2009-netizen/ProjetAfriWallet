using AfriWallet.Fraud.Investigation.Domain.Cases;

namespace AfriWallet.Fraud.Investigation.Application.Cases;

public sealed record FraudCaseResult(Guid CaseId, string Awid, Guid TransactionId, string Title, FraudCasePriority Priority, FraudCaseStatus Status, FraudCaseResolution Resolution, string? AnalystId, int EvidenceCount, int NoteCount, int ResponseCount, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);