namespace AfriWallet.Disputes.Eligibility.Application.Abstractions;

public sealed record DisputeEligibilityAuditEvent(
    Guid Id,
    Guid DecisionId,
    Guid ClaimId,
    string Awid,
    string Status,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyDictionary<string, string> Metadata);

public interface IDisputeEligibilityAuditStore
{
    Task AppendAsync(DisputeEligibilityAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DisputeEligibilityAuditEvent>> GetAsync(Guid decisionId, CancellationToken cancellationToken = default);
}
