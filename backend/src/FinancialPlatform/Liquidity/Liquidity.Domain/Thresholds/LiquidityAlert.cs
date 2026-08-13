namespace Liquidity.Domain.Thresholds;

public enum LiquidityAlertLevel
{
    None = 0,
    Healthy = 0,
    Warning = 1,
    Critical = 2
}

public sealed record LiquidityAlert(
    Guid AccountId,
    string CurrencyCode,
    LiquidityAlertLevel Level,
    long NetMinor,
    long AvailableMinor,
    DateTime EvaluatedAtUtc,
    string Reason);
