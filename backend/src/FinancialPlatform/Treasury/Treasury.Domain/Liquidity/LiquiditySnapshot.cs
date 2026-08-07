namespace Treasury.Domain.Liquidity;

public sealed class LiquiditySnapshot
{
    public LiquiditySnapshot(
        DateTime capturedAtUtc,
        decimal totalAvailable,
        decimal totalReserved)
    {
        CapturedAtUtc = capturedAtUtc;
        TotalAvailable = totalAvailable;
        TotalReserved = totalReserved;
    }

    public DateTime CapturedAtUtc { get; }

    public decimal TotalAvailable { get; }

    public decimal TotalReserved { get; }
}
