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

    public void MarkExecuted(bool allSettled)
    {
        Status = allSettled ? SettlementBatchStatus.Settled : SettlementBatchStatus.PartiallySettled;
        ExecutedAtUtc = DateTime.UtcNow;
    }
}
