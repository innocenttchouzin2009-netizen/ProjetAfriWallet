using AfriWallet.Merchants.Settlement.Domain.Settlements;

namespace AfriWallet.Merchants.Settlement.Application.Abstractions;

public sealed record CaptureEligibleDecisionSnapshot(
    Guid DecisionId,
    Guid PaymentIntentId,
    string MerchantId,
    string DecisionType,
    string DecisionStatus,
    long AmountMinor,
    string Currency,
    string MerchantRegistryStatus,
    string MerchantVerificationStatus);

public interface ICaptureEligibleDecisionReader
{
    Task<CaptureEligibleDecisionSnapshot?> GetAsync(Guid decisionId, CancellationToken ct = default);
}

public interface IMerchantSettlementRepository
{
    Task AddAsync(MerchantSettlementOrchestration settlement, CancellationToken ct = default);
    Task SaveAsync(MerchantSettlementOrchestration settlement, CancellationToken ct = default);
    Task<MerchantSettlementOrchestration?> GetAsync(Guid id, CancellationToken ct = default);
    Task<MerchantSettlementOrchestration?> GetByDecisionAsync(Guid id, CancellationToken ct = default);
    Task<MerchantSettlementOrchestration?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default);
}

public enum MerchantSettlementProviderStatus
{
    Accepted = 0,
    TemporaryFailure = 1,
    PermanentFailure = 2,
    Timeout = 3,
    PartialFailure = 4
}

public sealed record MerchantSettlementProviderRequest(
    Guid SettlementId,
    Guid PaymentDecisionId,
    Guid PaymentIntentId,
    string MerchantId,
    MerchantSettlementRoute Route,
    long AmountMinor,
    string Currency,
    string IdempotencyKey,
    string CorrelationId);

public sealed record MerchantSettlementProviderResult(
    MerchantSettlementProviderStatus Status,
    string? ProviderReference,
    string Message);

public sealed record MerchantSettlementAccountRoute(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    string Currency);

public interface IMerchantSettlementAccountResolver
{
    Task<MerchantSettlementAccountRoute?> ResolveAsync(
        string merchantId,
        MerchantSettlementRoute route,
        string currency,
        CancellationToken ct = default);
}

public interface IMerchantSettlementProvider
{
    Task<MerchantSettlementProviderResult> SubmitAsync(
        MerchantSettlementProviderRequest request,
        CancellationToken ct = default);

    Task<bool> CompensateAsync(string providerReference, CancellationToken ct = default);
}

public sealed record MerchantSettlementAuditEvent(
    Guid EventId,
    Guid SettlementId,
    Guid PaymentDecisionId,
    string MerchantId,
    string EventType,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyDictionary<string, string> Metadata);

public interface IMerchantSettlementAuditStore
{
    Task AppendAsync(MerchantSettlementAuditEvent auditEvent, CancellationToken ct = default);
    Task<IReadOnlyCollection<MerchantSettlementAuditEvent>> GetAsync(Guid id, CancellationToken ct = default);
}

public interface IMerchantSettlementClock
{
    DateTimeOffset UtcNow { get; }
}
