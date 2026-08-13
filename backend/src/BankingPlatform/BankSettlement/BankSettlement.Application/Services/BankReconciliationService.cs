using AfriWallet.BankingPlatform.BankSettlement.Domain.Reconciliation;

namespace AfriWallet.BankingPlatform.BankSettlement.Application.Services;

public sealed class BankReconciliationService
{
    private readonly IReconciliationRepository _reconciliationRepository;
    private readonly IBankSettlementRepository _bankSettlementRepository;

    public BankReconciliationService(
        IReconciliationRepository reconciliationRepository,
        IBankSettlementRepository bankSettlementRepository)
    {
        _reconciliationRepository = reconciliationRepository;
        _bankSettlementRepository = bankSettlementRepository;
    }

    public async Task<ReconciliationResult> ReconcileAsync(
        ReconciliationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var batch = await _bankSettlementRepository.GetByIdAsync(
            request.SettlementBatchId,
            cancellationToken);

        if (batch is null)
            throw new KeyNotFoundException(
                $"Settlement batch {request.SettlementBatchId} was not found.");

        if (batch.Status != Domain.Settlements.BankSettlementStatus.Closed &&
            batch.Status != Domain.Settlements.BankSettlementStatus.Reconciled)
        {
            throw new InvalidOperationException(
                "Only closed or reconciled settlement batches can be reconciled.");
        }

        var record = new ReconciliationRecord(
            Guid.NewGuid(),
            request.SettlementBatchId,
            request.ExpectedAmountMinor,
            request.ReportedAmountMinor,
            request.CurrencyCode,
            request.ExternalReference);

        await _reconciliationRepository.SaveAsync(record, cancellationToken);

        if (record.Status == ReconciliationStatus.Mismatched)
        {
            return new ReconciliationResult(
                record.ReconciliationId,
                record.SettlementBatchId,
                record.ExpectedAmountMinor,
                record.ReportedAmountMinor,
                record.DifferenceMinor,
                record.CurrencyCode,
                record.Status,
                false);
        }

        return new ReconciliationResult(
            record.ReconciliationId,
            record.SettlementBatchId,
            record.ExpectedAmountMinor,
            record.ReportedAmountMinor,
            record.DifferenceMinor,
            record.CurrencyCode,
            record.Status,
            true);
    }

    public async Task<ReconciliationResult> ResolveMismatchAsync(
        Guid reconciliationId,
        CancellationToken cancellationToken)
    {
        var record = await _reconciliationRepository.GetByIdAsync(
            reconciliationId,
            cancellationToken);

        if (record is null)
            throw new KeyNotFoundException(
                $"Reconciliation record {reconciliationId} was not found.");

        record.Resolve();
        await _reconciliationRepository.SaveAsync(record, cancellationToken);

        return new ReconciliationResult(
            record.ReconciliationId,
            record.SettlementBatchId,
            record.ExpectedAmountMinor,
            record.ReportedAmountMinor,
            record.DifferenceMinor,
            record.CurrencyCode,
            record.Status,
            true);
    }

    public async Task<IReadOnlyCollection<ReconciliationResult>> GetForBatchAsync(
        Guid settlementBatchId,
        CancellationToken cancellationToken)
    {
        var records = await _reconciliationRepository.GetForBatchAsync(
            settlementBatchId,
            cancellationToken);

        return records.Select(x => new ReconciliationResult(
            x.ReconciliationId,
            x.SettlementBatchId,
            x.ExpectedAmountMinor,
            x.ReportedAmountMinor,
            x.DifferenceMinor,
            x.CurrencyCode,
            x.Status,
            x.Status == ReconciliationStatus.Resolved)).ToList();
    }
}
