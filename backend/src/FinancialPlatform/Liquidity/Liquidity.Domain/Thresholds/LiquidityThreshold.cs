namespace Liquidity.Domain.Thresholds;

public sealed class LiquidityThreshold
{
    public long MinimumMinor { get; init; }

    public long WarningMinor { get; init; }

    public long CriticalMinor { get; init; }
}
