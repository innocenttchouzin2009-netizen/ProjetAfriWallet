using AfriWallet.Merchants.Payout.Domain;

namespace AfriWallet.Merchants.Payout.Application;

public sealed record MerchantReceivableSnapshot(
    Guid ReceivableId,
    string MerchantId,
    long AmountMinor,
    string Currency,
    string CaptureStatus,
    bool SettlementReady,
    string? CaptureProviderReference);

public interface IMerchantReceivableStore
{
    Task SaveAsync(MerchantReceivableSnapshot receivable, CancellationToken cancellationToken = default);
    Task<MerchantReceivableSnapshot?> GetAsync(Guid receivableId, CancellationToken cancellationToken = default);
}

public interface IMerchantPayoutDestinationRepository
{
    Task AddAsync(MerchantPayoutDestination destination, CancellationToken cancellationToken = default);
    Task SaveAsync(MerchantPayoutDestination destination, CancellationToken cancellationToken = default);
    Task<MerchantPayoutDestination?> GetAsync(Guid destinationId, CancellationToken cancellationToken = default);
}

public interface IMerchantPayoutRepository
{
    Task AddAsync(MerchantPayoutExecution payout, CancellationToken cancellationToken = default);
    Task SaveAsync(MerchantPayoutExecution payout, CancellationToken cancellationToken = default);
    Task<MerchantPayoutExecution?> GetAsync(Guid payoutId, CancellationToken cancellationToken = default);
    Task<MerchantPayoutExecution?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<MerchantPayoutExecution?> GetByReceivableAsync(Guid receivableId, CancellationToken cancellationToken = default);
}

public sealed record MerchantPayoutProviderRequest(
    Guid PayoutId,
    Guid ReceivableId,
    string MerchantId,
    long AmountMinor,
    string Currency,
    MerchantPayoutDestinationType DestinationType,
    string DestinationReference,
    string IdempotencyKey);

public sealed record MerchantPayoutProviderResult(
    bool Succeeded,
    string? ProviderReference,
    string? FailureCode);

public interface IMerchantPayoutProvider
{
    Task<MerchantPayoutProviderResult> ExecuteAsync(
        MerchantPayoutProviderRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record MerchantPayoutAuditEvent(
    Guid EventId,
    Guid PayoutId,
    Guid ReceivableId,
    string MerchantId,
    string EventType,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyDictionary<string,string> Metadata);

public interface IMerchantPayoutAuditStore
{
    Task AppendAsync(MerchantPayoutAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MerchantPayoutAuditEvent>> ListAsync(Guid payoutId, CancellationToken cancellationToken = default);
}
