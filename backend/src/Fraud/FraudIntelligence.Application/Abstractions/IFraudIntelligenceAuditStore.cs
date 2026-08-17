namespace AfriWallet.Fraud.Intelligence.Application.Abstractions;

public sealed record FraudIntelligenceAuditEvent(Guid Id, Guid FindingId, string Awid, string EventType, string Actor, DateTimeOffset OccurredAtUtc, IReadOnlyDictionary<string, string> Metadata);

public interface IFraudIntelligenceAuditStore
{
    Task AppendAsync(FraudIntelligenceAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<FraudIntelligenceAuditEvent>> GetAsync(Guid findingId, CancellationToken cancellationToken = default);
}