namespace Liquidity.Domain.Positions;

public sealed record LiquidityPosition(
    Guid AccountId,
    string CurrencyCode,
    long AvailableMinor,
    long ReservedMinor,
    long PendingMinor,
    long BlockedMinor,
    long NetMinor,
    DateTime CalculatedAtUtc);
