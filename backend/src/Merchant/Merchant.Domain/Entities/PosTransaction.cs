namespace AfriWallet.Merchant.Domain.Entities;

public sealed class PosTransaction
{
    public string TransactionId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string TerminalId { get; set; } = string.Empty;
    public string? TransferIntentId { get; set; }
    public string? ReceiptId { get; set; }
    public decimal AmountMinor { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public PosChannel Channel { get; set; }
    public PosTransactionStatus Status { get; set; } = PosTransactionStatus.Initiated;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
