using Microsoft.Extensions.Options;
using MobileMoney.Production.Correlation;

namespace MobileMoney.Production.FeatureFlags;

public sealed class MobileMoneyFeatureManager : IMobileMoneyFeatureManager
{
    private readonly Dictionary<string, bool> _flags;

    public MobileMoneyFeatureManager(IOptions<FeatureFlagOptions> options)
    {
        _flags = new Dictionary<string, bool>(options.Value.Flags, StringComparer.OrdinalIgnoreCase);
    }

    public MobileMoneyFeatureManager(FeatureFlagOptions options)
    {
        _flags = new Dictionary<string, bool>(options.Flags, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsEnabled(string featureName) => IsEnabled(featureName, false);

    public bool IsEnabled(string featureName, bool defaultValue)
    {
        return _flags.TryGetValue(featureName, out var enabled) ? enabled : defaultValue;
    }

    public bool IsProductionEnabled() => IsEnabled(MobileMoneyFeatureNames.MtnMomoProductionEnabled, false) && IsEnabled(MobileMoneyFeatureNames.MtnMomoEnabled, false);

    public bool IsSandboxEnabled() => IsEnabled(MobileMoneyFeatureNames.MtnMomoSandboxEnabled, false) && IsEnabled(MobileMoneyFeatureNames.MtnMomoEnabled, false);

    public bool IsFeatureAllowed(string featureName, string? correlationId = null)
    {
        if (!IsEnabled(MobileMoneyFeatureNames.MtnMomoEnabled, false))
        {
            return false;
        }

        if (featureName.Equals(MobileMoneyFeatureNames.MtnMomoProductionEnabled, StringComparison.OrdinalIgnoreCase))
        {
            return IsProductionEnabled();
        }

        if (featureName.Equals(MobileMoneyFeatureNames.MtnMomoSandboxEnabled, StringComparison.OrdinalIgnoreCase))
        {
            return IsSandboxEnabled();
        }

        return IsEnabled(featureName, false);
    }

    public MobileMoneyFeatureSnapshot GetSnapshot()
    {
        return new MobileMoneyFeatureSnapshot
        {
            Flags = new Dictionary<string, bool>(_flags, StringComparer.OrdinalIgnoreCase),
            IsProductionEnabled = IsProductionEnabled(),
            IsSandboxEnabled = IsSandboxEnabled(),
            IsMasterEnabled = IsEnabled(MobileMoneyFeatureNames.MtnMomoEnabled, false)
        };
    }

    public void SetFlag(string featureName, bool enabled)
    {
        _flags[featureName] = enabled;
    }
}
