namespace MerchantSettlement.Domain.Settlements;

public sealed class MerchantSettlement
{
    public MerchantSettlement(
        Guid settlementId,
        string merchantId,
        string currencyCode,
        long grossMinor,
        long feesMinor,
        long refundsMinor,
        long adjustmentsMinor,
        long reserveMinor,
        string idempotencyKey)
    {
        if (settlementId == Guid.Empty)
            throw new ArgumentException("Settlement ID is required.");

        SettlementId = settlementId;
        MerchantId = Require(merchantId);
        CurrencyCode = Require(currencyCode).ToUpperInvariant();

        GrossMinor = grossMinor;
        FeesMinor = feesMinor;
        RefundsMinor = refundsMinor;
        AdjustmentsMinor = adjustmentsMinor;
        ReserveMinor = reserveMinor;

        IdempotencyKey = Require(idempotencyKey);

        NetPayableMinor = checked(
            grossMinor - feesMinor - refundsMinor + adjustmentsMinor - reserveMinor);

        if (NetPayableMinor < 0)
            throw new InvalidOperationException("Merchant settlement cannot produce a negative payable.");
    }

    public Guid SettlementId { get; }

    public string MerchantId { get; }

    public string CurrencyCode { get; }

    public long GrossMinor { get; }

    public long FeesMinor { get; }

    public long RefundsMinor { get; }

    public long AdjustmentsMinor { get; }

    public long ReserveMinor { get; }

    public long NetPayableMinor { get; }

    public string IdempotencyKey { get; }

    public string? FinancialSettlementReference { get; private set; }

    public MerchantSettlementStatus Status { get; private set; }
        = MerchantSettlementStatus.Created;

    public DateTime CreatedAtUtc { get; }
        = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; private set; }

    public void Start()
    {
        if (Status != MerchantSettlementStatus.Created)
            throw new InvalidOperationException("Settlement cannot start.");

        Status = MerchantSettlementStatus.Processing;
    }

    public void AttachFinancialSettlement(string reference)
    {
        if (Status != MerchantSettlementStatus.Processing)
            throw new InvalidOperationException("Settlement is not processing.");

        FinancialSettlementReference = Require(reference);
    }

    public void Complete()
    {
        if (Status != MerchantSettlementStatus.Processing)
            throw new InvalidOperationException("Only processing settlement may complete.");

        if (FinancialSettlementReference is null)
            throw new InvalidOperationException("Financial settlement reference is missing.");

        Status = MerchantSettlementStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void Fail()
    {
        if (Status == MerchantSettlementStatus.Completed)
            throw new InvalidOperationException("Completed settlement is immutable.");

        Status = MerchantSettlementStatus.Failed;
    }

    private static string Require(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.");

        return value.Trim();
    }
}

public enum MerchantSettlementStatus
{
    Created,
    Processing,
    Completed,
    Failed,
    Reconciled,
    Exception
}
