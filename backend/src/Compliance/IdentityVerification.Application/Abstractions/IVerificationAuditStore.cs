namespace AfriWallet.Compliance.IdentityVerification.Application.Abstractions;

public sealed record VerificationAuditEvent(
    Guid Id,
    Guid SessionId,
    Guid ComplianceProfileId,
    string EventType,
    string Actor,
    DateTimeOffset OccurredAtUtc);

public interface IVerificationAuditStore
{
    Task AppendAsync(VerificationAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<VerificationAuditEvent>> GetBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
