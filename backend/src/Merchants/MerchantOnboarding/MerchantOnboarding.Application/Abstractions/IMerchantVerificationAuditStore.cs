namespace AfriWallet.Merchants.Onboarding.Application.Abstractions;

public sealed record MerchantVerificationAuditEvent(
    Guid EventId,
    Guid VerificationId,
    string MerchantId,
    string EventType,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyDictionary<string, string> Metadata);

public interface IMerchantVerificationAuditStore
{
    Task AppendAsync(MerchantVerificationAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MerchantVerificationAuditEvent>> GetAsync(Guid verificationId, CancellationToken cancellationToken = default);
}
