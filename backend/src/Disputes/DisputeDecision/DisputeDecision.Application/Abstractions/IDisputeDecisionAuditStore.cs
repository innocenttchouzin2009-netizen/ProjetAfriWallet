namespace AfriWallet.Disputes.Decision.Application.Abstractions;

public sealed record DisputeDecisionAuditEvent(
    Guid EventId,
    Guid DecisionId,
    Guid ClaimId,
    Guid InvestigationId,
    string Awid,
    string EventType,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyDictionary<string, string> Metadata);

public interface IDisputeDecisionAuditStore
{
    Task AppendAsync(DisputeDecisionAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DisputeDecisionAuditEvent>> GetAsync(Guid decisionId, CancellationToken cancellationToken = default);
}
