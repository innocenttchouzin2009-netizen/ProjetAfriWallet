using AfriWallet.Merchants.Capture.Domain;

namespace AfriWallet.Merchants.Capture.Application;

public sealed record CaptureEligibleDecisionSnapshot(Guid DecisionId,Guid PaymentIntentId,string MerchantId,string DecisionType,string DecisionStatus,long AmountMinor,string Currency,string MerchantRegistryStatus,string MerchantVerificationStatus);
public interface ICaptureEligibleDecisionReader{Task<CaptureEligibleDecisionSnapshot?> GetAsync(Guid decisionId,CancellationToken cancellationToken=default);}
public interface IMerchantCaptureRepository{Task AddAsync(MerchantCaptureExecution execution,CancellationToken cancellationToken=default);Task SaveAsync(MerchantCaptureExecution execution,CancellationToken cancellationToken=default);Task<MerchantCaptureExecution?> GetAsync(Guid executionId,CancellationToken cancellationToken=default);Task<MerchantCaptureExecution?> GetByIdempotencyKeyAsync(string idempotencyKey,CancellationToken cancellationToken=default);Task<MerchantCaptureExecution?> GetByDecisionAsync(Guid decisionId,CancellationToken cancellationToken=default);}
public sealed record MerchantCaptureProviderRequest(Guid ExecutionId,Guid DecisionId,Guid PaymentIntentId,string MerchantId,long AmountMinor,string Currency,string IdempotencyKey);
public sealed record MerchantCaptureProviderResult(bool Succeeded,string? ProviderReference,string? FailureCode);
public interface IMerchantCaptureProvider{Task<MerchantCaptureProviderResult> CaptureAsync(MerchantCaptureProviderRequest request,CancellationToken cancellationToken=default);}
public sealed record MerchantCaptureAuditEvent(Guid EventId,Guid ExecutionId,Guid DecisionId,Guid PaymentIntentId,string MerchantId,string EventType,string Actor,DateTimeOffset OccurredAtUtc,IReadOnlyDictionary<string,string> Metadata);
public interface IMerchantCaptureAuditStore{Task AppendAsync(MerchantCaptureAuditEvent auditEvent,CancellationToken cancellationToken=default);Task<IReadOnlyCollection<MerchantCaptureAuditEvent>> ListAsync(Guid executionId,CancellationToken cancellationToken=default);}
