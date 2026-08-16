namespace AfriWallet.Compliance.CaseManagement.Application.Abstractions;

public sealed record ComplianceCaseAuditEvent(
    Guid Id,
    Guid CaseId,
    string Awid,
    string EventType,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyDictionary<string, string> Metadata);

public interface IComplianceCaseAuditStore
{
    Task AppendAsync(ComplianceCaseAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ComplianceCaseAuditEvent>> GetByCaseAsync(Guid caseId, CancellationToken cancellationToken = default);
}