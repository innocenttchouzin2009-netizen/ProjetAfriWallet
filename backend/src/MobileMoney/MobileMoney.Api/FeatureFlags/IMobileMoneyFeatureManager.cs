namespace MobileMoney.Production.FeatureFlags;

public interface IMobileMoneyFeatureManager
{
    bool IsEnabled(string featureName);
    bool IsEnabled(string featureName, bool defaultValue);
    bool IsProductionEnabled();
    bool IsSandboxEnabled();
    bool IsFeatureAllowed(string featureName, string? correlationId = null);
    MobileMoneyFeatureSnapshot GetSnapshot();
    void SetFlag(string featureName, bool enabled);
}
