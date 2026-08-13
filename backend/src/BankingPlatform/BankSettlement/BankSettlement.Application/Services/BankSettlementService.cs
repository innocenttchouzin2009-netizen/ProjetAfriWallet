using AfriWallet.BankingPlatform.BankSettlement.Domain.Settlements;

namespace AfriWallet.BankingPlatform.BankSettlement.Application.Services;

public sealed class BankSettlementService
{
    private readonly IBankSettlementRepository _repository;
    private readonly IBankExecutionGateway _bankExecutionGateway;

    public BankSettlementService(
        IBankSettlementRepository repository,
        IBankExecutionGateway bankExecutionGateway)
    {
        _repository = repository;
        _bankExecutionGateway = bankExecutionGateway;
    }

    public async Task<SettlementBatchResult> CreateBatchAsync(
        CreateSettlementBatchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await _repository.GetByIdempotencyKeyAsync(
            request.IdempotencyKey,
            cancellationToken);

        if (existing is not null)
        {
            return Map(existing);
        }

        var batch = new BankSettlementBatch(
            Guid.NewGuid(),
            request.ProviderCode,
            request.RailCode,
            request.CurrencyCode,
            request.SettlementDate,
            request.IdempotencyKey);

        await _repository.SaveAsync(batch, cancellationToken);
        return Map(batch);
    }

    public async Task<SettlementBatchResult> AddItemAsync(
        Guid settlementBatchId,
        AddSettlementItemRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var batch = await _repository.GetByIdAsync(
            settlementBatchId,
            cancellationToken);

        if (batch is null)
            throw new KeyNotFoundException(
                $"Settlement batch {settlementBatchId} was not found.");

        var execution = await _bankExecutionGateway.GetExecutionAsync(
            request.ExecutionId,
            cancellationToken);

        if (execution is null)
            throw new InvalidOperationException(
                $"Execution {request.ExecutionId} is not available for settlement.");

        if (!string.Equals(
                execution.ProviderCode,
                request.ProviderCode,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Provider mismatch between execution and settlement item.");
        }

        if (!string.Equals(
                execution.RailCode,
                request.RailCode,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Rail mismatch between execution and settlement item.");
        }

        if (execution.AmountMinor != request.AmountMinor)
        {
            throw new InvalidOperationException(
                "Execution amount does not match settlement entry.");
        }

        var item = new BankSettlementItem(
            Guid.NewGuid(),
            request.ExecutionId,
            request.ProviderCode,
            request.RailCode,
            request.AmountMinor,
            request.FeeMinor,
            request.CurrencyCode,
            execution.ProviderReference ?? request.ProviderReference);

        batch.AddItem(item);
        await _repository.SaveAsync(batch, cancellationToken);

        return Map(batch);
    }

    public async Task<SettlementBatchResult> CloseBatchAsync(
        Guid settlementBatchId,
        CancellationToken cancellationToken)
    {
        var batch = await _repository.GetByIdAsync(
            settlementBatchId,
            cancellationToken);

        if (batch is null)
            throw new KeyNotFoundException(
                $"Settlement batch {settlementBatchId} was not found.");

        batch.Close();
        await _repository.SaveAsync(batch, cancellationToken);

        return Map(batch);
    }

    public async Task<IReadOnlyCollection<SettlementBatchResult>> GetOpenBatchesAsync(
        CancellationToken cancellationToken)
    {
        var batches = await _repository.GetOpenBatchesAsync(cancellationToken);
        return batches.Select(Map).ToList();
    }

    private static SettlementBatchResult Map(BankSettlementBatch batch)
    {
        return new SettlementBatchResult(
            batch.SettlementBatchId,
            batch.ProviderCode,
            batch.RailCode,
            batch.CurrencyCode,
            batch.SettlementDate,
            batch.IdempotencyKey,
            batch.Status,
            batch.GrossAmountMinor,
            batch.TotalFeesMinor,
            batch.NetAmountMinor,
            batch.Items);
    }
}
