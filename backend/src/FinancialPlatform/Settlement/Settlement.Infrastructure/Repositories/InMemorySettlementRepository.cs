using Settlement.Application.Interfaces;
using Settlement.Domain.Batches;
using Settlement.Domain.Instructions;

namespace Settlement.Infrastructure.Repositories;

public sealed class InMemorySettlementRepository : ISettlementRepository
{
    private readonly Dictionary<Guid, SettlementInstruction> _instructions = [];
    private readonly Dictionary<Guid, SettlementBatch> _batches = [];

    public Task SaveInstructionAsync(SettlementInstruction instruction, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _instructions[instruction.InstructionId] = instruction;
        return Task.CompletedTask;
    }

    public Task<SettlementInstruction?> GetInstructionAsync(Guid instructionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _instructions.TryGetValue(instructionId, out var instruction);
        return Task.FromResult(instruction);
    }

    public Task<IReadOnlyCollection<SettlementInstruction>> GetInstructionsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<SettlementInstruction>>(_instructions.Values.OrderBy(x => x.CreatedAtUtc).ToArray());
    }

    public Task SaveBatchAsync(SettlementBatch batch, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _batches[batch.BatchId] = batch;
        return Task.CompletedTask;
    }

    public Task<SettlementBatch?> GetBatchAsync(Guid batchId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _batches.TryGetValue(batchId, out var batch);
        return Task.FromResult(batch);
    }
}
