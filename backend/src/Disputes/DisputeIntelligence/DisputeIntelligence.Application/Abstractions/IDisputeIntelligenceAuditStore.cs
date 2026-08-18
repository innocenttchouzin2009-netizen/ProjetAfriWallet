namespace AfriWallet.Disputes.Intelligence.Application.Abstractions;

public sealed record DisputeIntelligenceAuditEvent(
    Guid EventId,
    Guid FindingId,
    string SubjectId,
    string EventType,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyDictionary<string, string> Metadata);

public interface IDisputeIntelligenceAuditStore
{
    Task AppendAsync(DisputeIntelligenceAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DisputeIntelligenceAuditEvent>> GetAsync(Guid findingId, CancellationToken cancellationToken = default);
}
