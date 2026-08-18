namespace AfriWallet.Merchants.Registry.Application.Abstractions;

public sealed record MerchantAuditEvent(
    Guid EventId,
    string MerchantId,
    string OwnerAwid,
    string EventType,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyDictionary<string, string> Metadata);

public interface IMerchantAuditStore
{
    Task AppendAsync(MerchantAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MerchantAuditEvent>> GetAsync(string merchantId, CancellationToken cancellationToken = default);
}
