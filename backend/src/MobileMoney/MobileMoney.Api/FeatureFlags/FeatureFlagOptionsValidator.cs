using Microsoft.Extensions.Options;

namespace MobileMoney.Production.FeatureFlags;

public sealed class FeatureFlagOptionsValidator : IValidateOptions<FeatureFlagOptions>
{
    public ValidateOptionsResult Validate(string? name, FeatureFlagOptions options)
    {
        if (options.Flags is null || options.Flags.Count == 0)
        {
            return ValidateOptionsResult.Fail("Feature flags must be configured.");
        }

        if (!options.Flags.TryGetValue(MobileMoneyFeatureNames.MtnMomoEnabled, out var enabled) || !enabled)
        {
            return ValidateOptionsResult.Success;
        }

        return ValidateOptionsResult.Success;
    }
}
