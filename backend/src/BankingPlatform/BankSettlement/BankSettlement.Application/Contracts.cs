using AfriWallet.BankingPlatform.BankSettlement.Domain.Reconciliation;
using AfriWallet.BankingPlatform.BankSettlement.Domain.Settlements;

namespace AfriWallet.BankingPlatform.BankSettlement.Application;

public sealed record CreateSettlementBatchRequest(
    string ProviderCode,
    string RailCode,
    string CurrencyCode,
    DateOnly SettlementDate,
    string IdempotencyKey);

public sealed record AddSettlementItemRequest(
    Guid ExecutionId,
    string ProviderCode,
    string RailCode,
    long AmountMinor,
    long FeeMinor,
    string CurrencyCode,
    string ProviderReference);

public sealed record SettlementBatchResult(
    Guid SettlementBatchId,
    string ProviderCode,
    string RailCode,
    string CurrencyCode,
    DateOnly SettlementDate,
    string IdempotencyKey,
    BankSettlementStatus Status,
    long GrossAmountMinor,
    long TotalFeesMinor,
    long NetAmountMinor,
    IReadOnlyCollection<BankSettlementItem> Items);

public sealed record ReconciliationRequest(
    Guid SettlementBatchId,
    long ExpectedAmountMinor,
    long ReportedAmountMinor,
    string CurrencyCode,
    string? ExternalReference = null);

public sealed record ReconciliationResult(
    Guid ReconciliationId,
    Guid SettlementBatchId,
    long ExpectedAmountMinor,
    long ReportedAmountMinor,
    long DifferenceMinor,
    string CurrencyCode,
    ReconciliationStatus Status,
    bool IsResolved);

public interface IBankSettlementRepository
{
    Task<BankSettlementBatch?> GetByIdAsync(Guid settlementBatchId, CancellationToken cancellationToken);
    Task<BankSettlementBatch?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task SaveAsync(BankSettlementBatch batch, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<BankSettlementBatch>> GetOpenBatchesAsync(CancellationToken cancellationToken);
}

public interface IReconciliationRepository
{
    Task<ReconciliationRecord?> GetByIdAsync(Guid reconciliationId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ReconciliationRecord>> GetForBatchAsync(Guid settlementBatchId, CancellationToken cancellationToken);
    Task SaveAsync(ReconciliationRecord record, CancellationToken cancellationToken);
}

public interface IBankExecutionGateway
{
    Task<BankExecutionStatusSnapshot?> GetExecutionAsync(Guid executionId, CancellationToken cancellationToken);
}

public sealed record BankExecutionStatusSnapshot(
    Guid ExecutionId,
    string ProviderCode,
    string RailCode,
    long AmountMinor,
    long FeeMinor,
    string CurrencyCode,
    string Status,
    string? ProviderReference);
