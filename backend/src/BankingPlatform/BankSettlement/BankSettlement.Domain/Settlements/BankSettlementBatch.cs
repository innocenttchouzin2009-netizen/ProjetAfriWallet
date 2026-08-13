namespace AfriWallet.BankingPlatform.BankSettlement.Domain.Settlements;

public sealed class BankSettlementBatch
{
    private readonly List<BankSettlementItem> _items = [];

    public BankSettlementBatch(
        Guid settlementBatchId,
        string providerCode,
        string railCode,
        string currencyCode,
        DateOnly settlementDate,
        string idempotencyKey)
    {
        if (settlementBatchId == Guid.Empty)
            throw new ArgumentException(
                "Settlement batch ID is required.");

        SettlementBatchId = settlementBatchId;
        ProviderCode = Require(providerCode);
        RailCode = Require(railCode);
        CurrencyCode = NormalizeCurrency(currencyCode);
        SettlementDate = settlementDate;
        IdempotencyKey = Require(idempotencyKey);
    }

    public Guid SettlementBatchId { get; }

    public string ProviderCode { get; }

    public string RailCode { get; }

    public string CurrencyCode { get; }

    public DateOnly SettlementDate { get; }

    public string IdempotencyKey { get; }

    public BankSettlementStatus Status { get; private set; }
        = BankSettlementStatus.Open;

    public DateTime CreatedAtUtc { get; }
        = DateTime.UtcNow;

    public DateTime? ClosedAtUtc { get; private set; }

    public IReadOnlyCollection<BankSettlementItem> Items =>
        _items.AsReadOnly();

    public long GrossAmountMinor =>
        _items.Sum(x => x.AmountMinor);

    public long TotalFeesMinor =>
        _items.Sum(x => x.FeeMinor);

    public long NetAmountMinor =>
        _items.Sum(x => x.NetAmountMinor);

    public void AddItem(BankSettlementItem item)
    {
        if (Status != BankSettlementStatus.Open)
            throw new InvalidOperationException(
                "Only open settlement batches can be modified.");

        if (!string.Equals(
                item.ProviderCode,
                ProviderCode,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Provider mismatch.");
        }

        if (!string.Equals(
                item.RailCode,
                RailCode,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Rail mismatch.");
        }

        if (!string.Equals(
                item.CurrencyCode,
                CurrencyCode,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Currency mismatch.");
        }

        if (_items.Any(x =>
                x.ExecutionId == item.ExecutionId))
        {
            throw new InvalidOperationException(
                "Execution already exists in settlement batch.");
        }

        _items.Add(item);
    }

    public void Close()
    {
        if (Status != BankSettlementStatus.Open)
            throw new InvalidOperationException(
                "Settlement batch is not open.");

        if (_items.Count == 0)
            throw new InvalidOperationException(
                "Empty settlement batch cannot be closed.");

        Status = BankSettlementStatus.Closed;
        ClosedAtUtc = DateTime.UtcNow;
    }

    public void MarkReconciled()
    {
        if (Status != BankSettlementStatus.Closed)
            throw new InvalidOperationException(
                "Only closed batches may be reconciled.");

        Status = BankSettlementStatus.Reconciled;
    }

    private static string Require(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.");

        return value.Trim();
    }

    private static string NormalizeCurrency(string value)
    {
        var result = Require(value).ToUpperInvariant();

        if (result.Length != 3)
            throw new ArgumentException(
                "Currency must use ISO 4217.");

        return result;
    }
}
