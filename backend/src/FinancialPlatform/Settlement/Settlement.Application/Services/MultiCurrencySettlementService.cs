using Settlement.Application.Interfaces;
using Settlement.Domain.Batches;
using Settlement.Domain.Instructions;

namespace Settlement.Application.Services;

public sealed class MultiCurrencySettlementService
{
    private readonly ISettlementRepository _repository;
    private readonly IFxQuoteProvider _fxQuoteProvider;
    private readonly ITreasurySettlementGateway _treasuryGateway;

    public MultiCurrencySettlementService(
        ISettlementRepository repository,
        IFxQuoteProvider fxQuoteProvider,
        ITreasurySettlementGateway treasuryGateway)
    {
        _repository = repository;
        _fxQuoteProvider = fxQuoteProvider;
        _treasuryGateway = treasuryGateway;
    }

    public async Task<SettlementInstruction> CreateInstructionAsync(
        Guid sourceAccountId,
        Guid destinationAccountId,
        string sourceCurrency,
        string destinationCurrency,
        long sourceAmountMinor,
        CancellationToken cancellationToken)
    {
        var normalizedSource = sourceCurrency.Trim().ToUpperInvariant();
        var normalizedDestination = destinationCurrency.Trim().ToUpperInvariant();

        var quote = normalizedSource == normalizedDestination
            ? null
            : await _fxQuoteProvider.GetQuoteAsync(
                normalizedSource,
                normalizedDestination,
                sourceAmountMinor,
                cancellationToken);

        var instruction = SettlementInstruction.Create(
            sourceAccountId,
            destinationAccountId,
            normalizedSource,
            normalizedDestination,
            sourceAmountMinor,
            quote);

        await _repository.SaveInstructionAsync(instruction, cancellationToken);
        return instruction;
    }

    public Task<SettlementInstruction?> GetInstructionAsync(Guid instructionId, CancellationToken cancellationToken)
    {
        return _repository.GetInstructionAsync(instructionId, cancellationToken);
    }

    public Task<IReadOnlyCollection<SettlementInstruction>> GetInstructionsAsync(CancellationToken cancellationToken)
    {
        return _repository.GetInstructionsAsync(cancellationToken);
    }

    public async Task<SettlementInstruction> ExecuteInstructionAsync(
        Guid instructionId,
        CancellationToken cancellationToken)
    {
        var instruction = await _repository.GetInstructionAsync(instructionId, cancellationToken)
            ?? throw new KeyNotFoundException("Settlement instruction not found.");

        if (instruction.Status == SettlementInstructionStatus.Settled)
        {
            return instruction;
        }

        if (instruction.Status == SettlementInstructionStatus.Rejected)
        {
            return instruction;
        }

        var hasFunds = await _treasuryGateway.HasAvailableFundsAsync(
            instruction.SourceAccountId,
            instruction.SourceCurrency,
            instruction.SourceAmountMinor,
            cancellationToken);

        if (!hasFunds)
        {
            instruction.MarkRejected("Insufficient available funds.");
            await _repository.SaveInstructionAsync(instruction, cancellationToken);
            return instruction;
        }

        var appliedRate = instruction.AppliedQuote?.Rate ?? 1m;

        await _treasuryGateway.PostSettlementAsync(
            new TreasurySettlementPosting(
                instruction.InstructionId,
                instruction.SourceAccountId,
                instruction.DestinationAccountId,
                instruction.SourceCurrency,
                instruction.DestinationCurrency,
                instruction.SourceAmountMinor,
                instruction.DestinationAmountMinor,
                appliedRate),
            cancellationToken);

        instruction.MarkSettled();
        await _repository.SaveInstructionAsync(instruction, cancellationToken);

        return instruction;
    }

    public async Task<SettlementBatch> CreateBatchAsync(
        IReadOnlyCollection<Guid> instructionIds,
        CancellationToken cancellationToken)
    {
        var instructions = new List<SettlementInstruction>();

        foreach (var instructionId in instructionIds)
        {
            var instruction = await _repository.GetInstructionAsync(instructionId, cancellationToken)
                ?? throw new KeyNotFoundException("Settlement instruction not found.");
            instructions.Add(instruction);
        }

        var sourceCurrency = instructions[0].SourceCurrency;
        var destinationCurrency = instructions[0].DestinationCurrency;

        var batch = SettlementBatch.Create(
            instructionIds,
            sourceCurrency,
            destinationCurrency,
            instructions.Sum(x => x.SourceAmountMinor),
            instructions.Sum(x => x.DestinationAmountMinor));

        await _repository.SaveBatchAsync(batch, cancellationToken);
        return batch;
    }

    public async Task<SettlementBatch> ExecuteBatchAsync(Guid batchId, CancellationToken cancellationToken)
    {
        var batch = await _repository.GetBatchAsync(batchId, cancellationToken)
            ?? throw new KeyNotFoundException("Settlement batch not found.");

        var allSettled = true;

        foreach (var instructionId in batch.InstructionIds)
        {
            var result = await ExecuteInstructionAsync(instructionId, cancellationToken);
            if (result.Status != SettlementInstructionStatus.Settled)
            {
                allSettled = false;
            }
        }

        batch.MarkExecuted(allSettled);
        await _repository.SaveBatchAsync(batch, cancellationToken);
        return batch;
    }
}
