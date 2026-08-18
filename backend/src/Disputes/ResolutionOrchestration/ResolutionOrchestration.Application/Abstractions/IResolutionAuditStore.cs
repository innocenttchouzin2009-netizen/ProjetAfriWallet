namespace AfriWallet.Disputes.Resolution.Application.Abstractions;

public sealed record ResolutionAuditEvent(
    Guid EventId,
    Guid ResolutionId,
    Guid DecisionId,
    string Awid,
    string EventType,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyDictionary<string, string> Metadata);

public interface IResolutionAuditStore
{
    Task AppendAsync(ResolutionAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ResolutionAuditEvent>> GetAsync(Guid resolutionId, CancellationToken cancellationToken = default);
}
