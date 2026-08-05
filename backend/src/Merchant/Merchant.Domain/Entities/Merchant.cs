namespace AfriWallet.Merchant.Domain.Entities;

public sealed class Merchant
{
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantCode { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public MerchantType MerchantType { get; set; } = MerchantType.Individual;
    public MerchantCategory MerchantCategoryCode { get; set; } = MerchantCategory.Retail;
    public string CountryCode { get; set; } = string.Empty;
    public string BaseCurrency { get; set; } = string.Empty;
    public string SettlementCurrency { get; set; } = string.Empty;
    public string BusinessRegistrationNumber { get; set; } = string.Empty;
    public string TaxIdentifier { get; set; } = string.Empty;
    public MerchantStatus Status { get; set; } = MerchantStatus.Pending;
    public MerchantCapabilities Capabilities { get; set; } = new();
    public string PreferredSettlementMethod { get; set; } = "WALLET";
    public string WalletId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int Version { get; set; } = 1;

    public bool CanAcceptPayments() => Status == MerchantStatus.Active;
}
