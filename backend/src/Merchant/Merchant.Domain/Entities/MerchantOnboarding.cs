namespace AfriWallet.Merchant.Domain.Entities;

public sealed class MerchantOnboarding
{
    public string MerchantId { get; set; } = string.Empty;
    public MerchantOnboardingStatus Status { get; set; } = MerchantOnboardingStatus.Draft;
    public MerchantProfile? Profile { get; set; }
    public MerchantKycCase? KycCase { get; set; }
    public List<string> AuditEvents { get; set; } = [];
    public List<string> TelemetryEvents { get; set; } = [];
}