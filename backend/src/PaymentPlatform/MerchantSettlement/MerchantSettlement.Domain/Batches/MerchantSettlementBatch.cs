namespace MerchantSettlement.Domain.Batches;

public sealed class MerchantSettlementBatch
{
    private readonly List<Guid> _settlementIds = [];

    public MerchantSettlementBatch(
        Guid batchId,
        string batchReference,
        string currencyCode,
        DateTime settlementDateUtc)
    {
        if (batchId == Guid.Empty)
            throw new ArgumentException("Batch ID is required.");

        BatchId = batchId;
        BatchReference = Require(batchReference);
        CurrencyCode = Require(currencyCode).ToUpperInvariant();
        SettlementDateUtc = settlementDateUtc;
    }

    public Guid BatchId { get; }

    public string BatchReference { get; }

    public string CurrencyCode { get; }

    public DateTime SettlementDateUtc { get; }

    public SettlementBatchStatus Status { get; private set; } = SettlementBatchStatus.Created;

    public IReadOnlyCollection<Guid> SettlementIds => _settlementIds.AsReadOnly();

    public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; private set; }

    public void AddSettlement(Guid settlementId)
    {
        if (Status != SettlementBatchStatus.Created)
            throw new InvalidOperationException("Only created batches may be modified.");

        if (!_settlementIds.Contains(settlementId))
            _settlementIds.Add(settlementId);
    }

    public void Start()
    {
        if (Status != SettlementBatchStatus.Created)
            throw new InvalidOperationException("Batch cannot start.");

        Status = SettlementBatchStatus.Processing;
    }

    public void Complete()
    {
        if (Status != SettlementBatchStatus.Processing)
            throw new InvalidOperationException("Only processing batches can complete.");

        Status = SettlementBatchStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    private static string Require(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.");

        return value.Trim();
    }
}

public enum SettlementBatchStatus
{
    Created,
    Processing,
    Completed,
    Failed
}
