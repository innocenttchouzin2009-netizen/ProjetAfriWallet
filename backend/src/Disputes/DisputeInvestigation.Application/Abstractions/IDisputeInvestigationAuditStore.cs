namespace AfriWallet.Disputes.Investigation.Application.Abstractions;

public sealed record DisputeInvestigationAuditEvent(
    Guid Id,
    Guid InvestigationId,
    Guid ClaimId,
    string Awid,
    string EventType,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyDictionary<string, string> Metadata);

public interface IDisputeInvestigationAuditStore
{
    Task AppendAsync(DisputeInvestigationAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DisputeInvestigationAuditEvent>> GetAsync(Guid investigationId, CancellationToken cancellationToken = default);
}
