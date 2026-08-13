namespace MerchantSettlement.Domain.Settlements;

public sealed class MerchantSettlementRecord
{
    public MerchantSettlementRecord(
        Guid settlementId,
        string merchantId,
        string currencyCode,
        long grossAmountMinor,
        long feeMinor,
        long netAmountMinor)
    {
        if (settlementId == Guid.Empty)
            throw new ArgumentException("Settlement ID is required.");

        if (string.IsNullOrWhiteSpace(merchantId))
            throw new ArgumentException("Merchant ID is required.");

        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("Currency is required.");

        if (grossAmountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(grossAmountMinor));

        SettlementId = settlementId;
        MerchantId = merchantId.Trim();
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        GrossAmountMinor = grossAmountMinor;
        FeeMinor = feeMinor;
        NetAmountMinor = netAmountMinor;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid SettlementId { get; }

    public string MerchantId { get; }

    public string CurrencyCode { get; }

    public long GrossAmountMinor { get; }

    public long FeeMinor { get; }

    public long NetAmountMinor { get; }

    public DateTime CreatedAtUtc { get; }

    public SettlementRecordStatus Status { get; private set; }
        = SettlementRecordStatus.Pending;

    public DateTime? SettledAtUtc { get; private set; }

    public void MarkSettled()
    {
        if (Status == SettlementRecordStatus.Settled)
            throw new InvalidOperationException("Settlement already settled.");

        Status = SettlementRecordStatus.Settled;
        SettledAtUtc = DateTime.UtcNow;
    }
}

public enum SettlementRecordStatus
{
    Pending,
    Settled,
    Failed
}
