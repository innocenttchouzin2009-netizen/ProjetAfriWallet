namespace Liquidity.Domain.Snapshots;

public sealed record LiquiditySnapshot(
    Guid SnapshotId,
    DateTime CreatedUtc,
    string Currency,
    long Available,
    long Reserved,
    long Pending,
    long Blocked,
    long Net);
