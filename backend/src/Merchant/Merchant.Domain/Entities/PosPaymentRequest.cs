namespace AfriWallet.Merchant.Domain.Entities;

public sealed class PosPaymentRequest
{
    public string MerchantId { get; set; } = string.Empty;
    public string TerminalId { get; set; } = string.Empty;
    public decimal AmountMinor { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public PosChannel Channel { get; set; }
    public string Description { get; set; } = string.Empty;
}
