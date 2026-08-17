namespace AfriWallet.Fraud.Intelligence.Application.Models;

public sealed record IntelligenceTransactionSnapshot(Guid TransactionId, string Awid, string DeviceId, string BeneficiaryId, decimal Amount, string Currency, int FraudScore, DateTimeOffset OccurredAtUtc);
public sealed record IntelligenceCaseSnapshot(Guid CaseId, string Awid, string Resolution, DateTimeOffset CreatedAtUtc);
public sealed record IntelligenceSourceSnapshot(string Awid, IReadOnlyCollection<IntelligenceTransactionSnapshot> Transactions, IReadOnlyCollection<IntelligenceCaseSnapshot> Cases);