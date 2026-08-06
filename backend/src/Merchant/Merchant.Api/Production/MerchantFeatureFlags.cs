namespace AfriWallet.Merchant.Api.Production;

public sealed class MerchantFeatureFlags
{
    public bool MerchantEnabled { get; set; } = true;
    public bool MerchantKycEnabled { get; set; } = true;
    public bool MerchantQrEnabled { get; set; } = true;
    public bool MerchantPosEnabled { get; set; } = true;
    public bool MerchantSettlementEnabled { get; set; } = true;
    public bool MerchantDashboardEnabled { get; set; } = true;
    public bool ProductionEnabled { get; set; } = false;
}
