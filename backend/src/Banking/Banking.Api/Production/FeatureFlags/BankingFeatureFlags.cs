namespace AfriWallet.Banking.Api.Production.FeatureFlags;

public sealed class BankingFeatureFlags
{
    public bool BankingEnabled { get; set; }
    public bool SepaEnabled { get; set; }
    public bool SwiftEnabled { get; set; }
    public bool DomesticEnabled { get; set; }
    public bool SandboxEnabled { get; set; }
    public bool ProductionEnabled { get; set; }
    public bool TimelineEnabled { get; set; }
    public bool NotificationsEnabled { get; set; }
}
