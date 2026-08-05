namespace AfriWallet.Merchant.Domain.Entities;

public sealed class MerchantKycRequirement
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}