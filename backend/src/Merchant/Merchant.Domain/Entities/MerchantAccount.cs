namespace AfriWallet.Merchant.Domain.Entities;

public sealed class MerchantAccount
{
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public string Environment { get; set; } = "Sandbox";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
