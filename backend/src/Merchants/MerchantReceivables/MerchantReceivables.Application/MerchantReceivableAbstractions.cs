using AfriWallet.Merchants.Receivables.Domain;

namespace AfriWallet.Merchants.Receivables.Application;

public sealed record CapturedMerchantPaymentSnapshot(
    Guid CaptureExecutionId,
    Guid DecisionId,
    Guid PaymentIntentId,
    string MerchantId,
    long AmountMinor,
    string Currency,
    string Status,
    string? ProviderReference);

public sealed record MerchantReceivableRegistrySnapshot(
    string MerchantId,
    string Status,
    string SettlementCurrency);

public interface IMerchantCaptureReceivableReader
{
    Task<CapturedMerchantPaymentSnapshot?> GetAsync(Guid captureExecutionId, CancellationToken cancellationToken = default);
}

public interface IMerchantRegistryReceivableReader
{
    Task<MerchantReceivableRegistrySnapshot?> GetAsync(string merchantId, CancellationToken cancellationToken = default);
}

public interface IMerchantFeeScheduleStore
{
    Task UpsertAsync(MerchantFeeSchedule schedule, CancellationToken cancellationToken = default);
    Task<MerchantFeeSchedule?> GetAsync(string merchantId, string currency, CancellationToken cancellationToken = default);
}

public interface IMerchantReceivableRepository
{
    Task AddAsync(MerchantReceivable receivable, CancellationToken cancellationToken = default);
    Task SaveAsync(MerchantReceivable receivable, CancellationToken cancellationToken = default);
    Task<MerchantReceivable?> GetAsync(Guid receivableId, CancellationToken cancellationToken = default);
    Task<MerchantReceivable?> GetByCaptureAsync(Guid captureExecutionId, CancellationToken cancellationToken = default);
    Task<MerchantReceivable?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MerchantReceivable>> ListOpenAsync(string merchantId, string currency, CancellationToken cancellationToken = default);
}

public sealed record MerchantReceivableAuditEvent(
    Guid EventId,
    Guid ReceivableId,
    Guid CaptureExecutionId,
    string MerchantId,
    string EventType,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyDictionary<string,string> Metadata);

public interface IMerchantReceivableAuditStore
{
    Task AppendAsync(MerchantReceivableAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MerchantReceivableAuditEvent>> ListAsync(Guid receivableId, CancellationToken cancellationToken = default);
}
