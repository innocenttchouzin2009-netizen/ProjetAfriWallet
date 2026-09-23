using AfriWallet.Merchants.Billing.Domain;

namespace AfriWallet.Merchants.Billing.Application;

public sealed record MerchantBillingCaptureSnapshot(
    Guid CaptureExecutionId,
    string MerchantId,
    string Status,
    bool SettlementReady);

public sealed record MerchantBillingReceivableSnapshot(
    Guid ReceivableId,
    Guid CaptureExecutionId,
    string MerchantId,
    long GrossAmountMinor,
    long FeeAmountMinor,
    long NetAmountMinor,
    string Currency,
    string Status);

public interface IMerchantBillingCaptureReader
{
    Task<MerchantBillingCaptureSnapshot?> GetAsync(
        Guid captureExecutionId,
        CancellationToken cancellationToken = default);
}

public interface IMerchantBillingReceivablePort
{
    Task<MerchantBillingReceivableSnapshot> CreateFromCaptureAsync(
        Guid captureExecutionId,
        string idempotencyKey,
        string actor,
        CancellationToken cancellationToken = default);
}

public interface IMerchantBillingHandoffRepository
{
    Task AddAsync(MerchantBillingHandoff handoff, CancellationToken cancellationToken = default);
    Task SaveAsync(MerchantBillingHandoff handoff, CancellationToken cancellationToken = default);
    Task<MerchantBillingHandoff?> GetAsync(Guid handoffId, CancellationToken cancellationToken = default);
    Task<MerchantBillingHandoff?> GetByCaptureAsync(Guid captureExecutionId, CancellationToken cancellationToken = default);
}

public sealed record MerchantBillingAuditEvent(
    Guid EventId,
    Guid HandoffId,
    Guid CaptureExecutionId,
    string MerchantId,
    string EventType,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyDictionary<string,string> Metadata);

public interface IMerchantBillingAuditStore
{
    Task AppendAsync(MerchantBillingAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MerchantBillingAuditEvent>> ListAsync(
        Guid handoffId,
        CancellationToken cancellationToken = default);
}
