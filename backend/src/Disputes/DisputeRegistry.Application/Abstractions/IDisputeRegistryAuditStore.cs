namespace AfriWallet.Disputes.Registry.Application.Abstractions;

public sealed record DisputeRegistryAuditEvent(
    Guid Id,
    Guid ClaimId,
    string Awid,
    Guid TransactionId,
    string EventType,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyDictionary<string, string> Metadata);

public interface IDisputeRegistryAuditStore
{
    Task AppendAsync(DisputeRegistryAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DisputeRegistryAuditEvent>> GetByClaimAsync(Guid claimId, CancellationToken cancellationToken = default);
}
