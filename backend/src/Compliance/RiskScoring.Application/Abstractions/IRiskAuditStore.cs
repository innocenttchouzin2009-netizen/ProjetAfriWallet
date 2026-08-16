namespace AfriWallet.Compliance.RiskScoring.Application.Abstractions;

public sealed record RiskAuditEvent(
    Guid Id,
    string Awid,
    Guid RiskProfileId,
    string EventType,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyDictionary<string, string> Metadata);

public interface IRiskAuditStore
{
    Task AppendAsync(RiskAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RiskAuditEvent>> GetByAwidAsync(
        string awid,
        CancellationToken cancellationToken = default);
}