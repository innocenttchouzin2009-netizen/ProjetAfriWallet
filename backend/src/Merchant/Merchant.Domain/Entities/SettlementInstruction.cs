namespace AfriWallet.Merchant.Domain.Entities;

public enum SettlementMethod
{
    AFRIWALLET_WALLET,
    BANK_TRANSFER,
    MTN_MOMO,
    ORANGE_MONEY
}

public enum SettlementStatus
{
    CREATED,
    SCHEDULED,
    PROCESSING,
    COMPLETED,
    FAILED,
    CANCELLED,
    REVERSED
}

public sealed class SettlementInstruction
{
    public string SettlementId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantWalletId { get; set; } = string.Empty;
    public string PaymentReference { get; set; } = string.Empty;
    public decimal GrossAmountMinor { get; set; }
    public decimal FeeAmountMinor { get; set; }
    public decimal TaxAmountMinor { get; set; }
    public decimal NetAmountMinor { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public SettlementMethod SettlementMethod { get; set; }
    public string DestinationAccount { get; set; } = string.Empty;
    public SettlementStatus Status { get; set; } = SettlementStatus.CREATED;
    public DateTimeOffset? ScheduledAt { get; set; }
    public DateTimeOffset? ExecutedAt { get; set; }
    public string? CorrelationId { get; set; }
    public string? TraceId { get; set; }
    public int Version { get; set; } = 1;
}
