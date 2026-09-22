using Microsoft.EntityFrameworkCore;
using Settlement.Application.Interfaces;
using Settlement.Domain.Batches;
using Settlement.Domain.Fx;
using Settlement.Domain.Instructions;
using Settlement.Infrastructure.Persistence;

namespace Settlement.Infrastructure.Repositories;

public sealed class EfSettlementRepository(SettlementDbContext dbContext) : ISettlementRepository
{
    public async Task SaveInstructionAsync(SettlementInstruction instruction, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Instructions.SingleOrDefaultAsync(x => x.InstructionId == instruction.InstructionId, cancellationToken);
        if (entity is null)
        {
            entity = new SettlementInstructionEntity { InstructionId = instruction.InstructionId };
            dbContext.Instructions.Add(entity);
        }

        Map(instruction, entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SettlementInstruction?> GetInstructionAsync(Guid instructionId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Instructions.AsNoTracking().SingleOrDefaultAsync(x => x.InstructionId == instructionId, cancellationToken);
        return entity is null ? null : Restore(entity);
    }

    public async Task<IReadOnlyCollection<SettlementInstruction>> GetInstructionsAsync(CancellationToken cancellationToken) =>
        (await dbContext.Instructions.AsNoTracking().OrderBy(x => x.CreatedAtUtc).ToListAsync(cancellationToken))
        .Select(Restore).ToArray();

    public async Task SaveBatchAsync(SettlementBatch batch, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Batches.SingleOrDefaultAsync(x => x.BatchId == batch.BatchId, cancellationToken);
        if (entity is null)
        {
            entity = new SettlementBatchEntity { BatchId = batch.BatchId };
            dbContext.Batches.Add(entity);
        }

        entity.InstructionIdsCsv = string.Join(',', batch.InstructionIds.Select(x => x.ToString("D")));
        entity.SourceCurrency = batch.SourceCurrency;
        entity.DestinationCurrency = batch.DestinationCurrency;
        entity.TotalSourceAmountMinor = batch.TotalSourceAmountMinor;
        entity.TotalDestinationAmountMinor = batch.TotalDestinationAmountMinor;
        entity.Status = (int)batch.Status;
        entity.CreatedAtUtc = batch.CreatedAtUtc;
        entity.ExecutedAtUtc = batch.ExecutedAtUtc;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SettlementBatch?> GetBatchAsync(Guid batchId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Batches.AsNoTracking().SingleOrDefaultAsync(x => x.BatchId == batchId, cancellationToken);
        return entity is null ? null : SettlementBatch.Restore(
            entity.BatchId,
            entity.InstructionIdsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToArray(),
            entity.SourceCurrency,
            entity.DestinationCurrency,
            entity.TotalSourceAmountMinor,
            entity.TotalDestinationAmountMinor,
            (SettlementBatchStatus)entity.Status,
            entity.CreatedAtUtc,
            entity.ExecutedAtUtc);
    }

    private static void Map(SettlementInstruction value, SettlementInstructionEntity entity)
    {
        entity.SourceAccountId = value.SourceAccountId;
        entity.DestinationAccountId = value.DestinationAccountId;
        entity.SourceCurrency = value.SourceCurrency;
        entity.DestinationCurrency = value.DestinationCurrency;
        entity.SourceAmountMinor = value.SourceAmountMinor;
        entity.DestinationAmountMinor = value.DestinationAmountMinor;
        entity.QuoteRate = value.AppliedQuote?.Rate;
        entity.QuoteAtUtc = value.AppliedQuote?.QuotedAtUtc;
        entity.QuoteExpiresAtUtc = value.AppliedQuote?.ExpiresAtUtc;
        entity.Status = (int)value.Status;
        entity.RejectionReason = value.RejectionReason;
        entity.CreatedAtUtc = value.CreatedAtUtc;
        entity.ExecutedAtUtc = value.ExecutedAtUtc;
    }

    private static SettlementInstruction Restore(SettlementInstructionEntity entity)
    {
        FxQuote? quote = entity.QuoteRate is null ? null : new(
            entity.SourceCurrency,
            entity.DestinationCurrency,
            entity.QuoteRate.Value,
            entity.QuoteAtUtc!.Value,
            entity.QuoteExpiresAtUtc!.Value);

        return SettlementInstruction.Restore(
            entity.InstructionId,
            entity.SourceAccountId,
            entity.DestinationAccountId,
            entity.SourceCurrency,
            entity.DestinationCurrency,
            entity.SourceAmountMinor,
            entity.DestinationAmountMinor,
            quote,
            (SettlementInstructionStatus)entity.Status,
            entity.RejectionReason,
            entity.CreatedAtUtc,
            entity.ExecutedAtUtc);
    }
}
