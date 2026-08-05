namespace AfriWallet.Merchant.Domain.Entities;

public sealed class MerchantKycCase
{
    public string MerchantId { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public MerchantKycStatus Status { get; set; } = MerchantKycStatus.InProgress;
    public List<MerchantKycRequirement> Requirements { get; set; } = [];
    public List<string> Decisions { get; set; } = [];
}