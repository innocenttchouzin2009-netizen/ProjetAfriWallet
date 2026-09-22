namespace Settlement.Domain.Batches;

public enum SettlementBatchStatus
{
    Open = 0,
    Settled = 1,
    PartiallySettled = 2
}

public sealed class SettlementBatch
{
    public Guid BatchId { get; private set; }

    public IReadOnlyCollection<Guid> InstructionIds { get; private set; } = [];

    public string SourceCurrency { get; private set; } = string.Empty;

    public string DestinationCurrency { get; private set; } = string.Empty;

    public long TotalSourceAmountMinor { get; private set; }

    public long TotalDestinationAmountMinor { get; private set; }

    public SettlementBatchStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ExecutedAtUtc { get; private set; }

    public static SettlementBatch Create(
        IReadOnlyCollection<Guid> instructionIds,
        string sourceCurrency,
        string destinationCurrency,
        long totalSourceAmountMinor,
        long totalDestinationAmountMinor)
    {
        if (instructionIds.Count == 0)
        {
            throw new ArgumentException("At least one instruction is required.", nameof(instructionIds));
        }

        return new SettlementBatch
        {
            BatchId = Guid.NewGuid(),
            InstructionIds = instructionIds,
            SourceCurrency = sourceCurrency,
            DestinationCurrency = destinationCurrency,
            TotalSourceAmountMinor = totalSourceAmountMinor,
            TotalDestinationAmountMinor = totalDestinationAmountMinor,
            Status = SettlementBatchStatus.Open,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public static SettlementBatch Restore(
        Guid batchId,
        IReadOnlyCollection<Guid> instructionIds,
        string sourceCurrency,
        string destinationCurrency,
        long totalSourceAmountMinor,
        long totalDestinationAmountMinor,
        SettlementBatchStatus status,
        DateTime createdAtUtc,
        DateTime? executedAtUtc)
    {
        if (batchId == Guid.Empty) throw new ArgumentException("Batch ID is required.", nameof(batchId));
        if (instructionIds is null || instructionIds.Count == 0) throw new ArgumentException("At least one instruction is required.", nameof(instructionIds));
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));

        return new SettlementBatch
        {
            BatchId = batchId,
            InstructionIds = instructionIds.ToArray(),
            SourceCurrency = sourceCurrency.Trim().ToUpperInvariant(),
            DestinationCurrency = destinationCurrency.Trim().ToUpperInvariant(),
            TotalSourceAmountMinor = totalSourceAmountMinor,
            TotalDestinationAmountMinor = totalDestinationAmountMinor,
            Status = status,
            CreatedAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc),
            ExecutedAtUtc = executedAtUtc is null ? null : DateTime.SpecifyKind(executedAtUtc.Value, DateTimeKind.Utc)
        };
    }

    public void MarkExecuted(bool allSettled)
    {
        Status = allSettled ? SettlementBatchStatus.Settled : SettlementBatchStatus.PartiallySettled;
        ExecutedAtUtc = DateTime.UtcNow;
    }
}
