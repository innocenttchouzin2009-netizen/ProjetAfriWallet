namespace AfriWallet.Fraud.Investigation.Application.Abstractions;

public sealed record FraudInvestigationAuditEvent(Guid Id, Guid CaseId, string Awid, Guid TransactionId, string EventType, string Actor, DateTimeOffset OccurredAtUtc, IReadOnlyDictionary<string, string> Metadata);

public interface IFraudInvestigationAuditStore
{
    Task AppendAsync(FraudInvestigationAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<FraudInvestigationAuditEvent>> GetByCaseAsync(Guid caseId, CancellationToken cancellationToken = default);
}