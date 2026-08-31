namespace AfriWallet.Merchants.Intelligence.Application.Abstractions;
public sealed record MerchantIntelligenceAuditEvent(Guid EventId, Guid FindingId, string MerchantId, string EventType, string Actor, DateTimeOffset OccurredAtUtc, IReadOnlyDictionary<string, string> Metadata);
public interface IMerchantIntelligenceAuditStore { Task AppendAsync(MerchantIntelligenceAuditEvent auditEvent, CancellationToken cancellationToken = default); Task<IReadOnlyCollection<MerchantIntelligenceAuditEvent>> GetAsync(Guid findingId, CancellationToken cancellationToken = default); }
