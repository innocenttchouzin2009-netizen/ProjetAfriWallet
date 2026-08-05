namespace AfriWallet.Merchant.Domain.Entities;

public sealed class PosReceipt
{
    public string ReceiptId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string TerminalId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Status { get; set; } = "Issued";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
