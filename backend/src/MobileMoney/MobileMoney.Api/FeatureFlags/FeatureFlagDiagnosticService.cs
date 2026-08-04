namespace MobileMoney.Production.FeatureFlags;

public sealed class FeatureFlagDiagnosticService
{
    private readonly IMobileMoneyFeatureManager _featureManager;

    public FeatureFlagDiagnosticService(IMobileMoneyFeatureManager featureManager)
    {
        _featureManager = featureManager;
    }

    public object GetDiagnosticSnapshot()
    {
        var snapshot = _featureManager.GetSnapshot();
        return new
        {
            masterEnabled = snapshot.IsMasterEnabled,
            sandboxEnabled = snapshot.IsSandboxEnabled,
            productionEnabled = snapshot.IsProductionEnabled,
            flags = snapshot.Flags.Where(x => !x.Key.Contains("Secret", StringComparison.OrdinalIgnoreCase)).ToDictionary(x => x.Key, x => x.Value)
        };
    }
}
