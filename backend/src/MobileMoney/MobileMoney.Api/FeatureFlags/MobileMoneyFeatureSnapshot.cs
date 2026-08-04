namespace MobileMoney.Production.FeatureFlags;

public sealed class MobileMoneyFeatureSnapshot
{
    public required IReadOnlyDictionary<string, bool> Flags { get; init; }
    public bool IsProductionEnabled { get; init; }
    public bool IsSandboxEnabled { get; init; }
    public bool IsMasterEnabled { get; init; }
}
