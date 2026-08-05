namespace AfriWallet.Merchant.Domain.Entities;

public sealed class BusinessProfile
{
    public string BusinessName { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string TaxIdentifier { get; set; } = string.Empty;
    public MerchantCategory MerchantCategoryCode { get; set; } = MerchantCategory.Retail;
    public string Description { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
}