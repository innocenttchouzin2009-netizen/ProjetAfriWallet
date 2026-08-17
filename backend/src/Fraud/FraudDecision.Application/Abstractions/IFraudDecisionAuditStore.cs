namespace AfriWallet.Fraud.Decision.Application.Abstractions;

public sealed record FraudDecisionAuditEvent(
    Guid Id,
    Guid DecisionId,
    Guid TransactionId,
    string Awid,
    string Action,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyDictionary<string, string> Metadata);

public interface IFraudDecisionAuditStore
{
    Task AppendAsync(FraudDecisionAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<FraudDecisionAuditEvent>> GetByDecisionAsync(Guid decisionId, CancellationToken cancellationToken = default);
}