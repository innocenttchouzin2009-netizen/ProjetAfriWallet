using Settlement.Domain.Batches;
using Settlement.Domain.Instructions;

namespace Settlement.Application.Interfaces;

public interface ISettlementRepository
{
    Task SaveInstructionAsync(SettlementInstruction instruction, CancellationToken cancellationToken);

    Task<SettlementInstruction?> GetInstructionAsync(Guid instructionId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SettlementInstruction>> GetInstructionsAsync(CancellationToken cancellationToken);

    Task SaveBatchAsync(SettlementBatch batch, CancellationToken cancellationToken);

    Task<SettlementBatch?> GetBatchAsync(Guid batchId, CancellationToken cancellationToken);
}
