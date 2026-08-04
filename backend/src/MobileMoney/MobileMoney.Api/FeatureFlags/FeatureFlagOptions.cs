namespace MobileMoney.Production.FeatureFlags;

public sealed class FeatureFlagOptions
{
    public const string SectionName = "FeatureFlags";

    public Dictionary<string, bool> Flags { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
