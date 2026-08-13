using Liquidity.Domain.Snapshots;

namespace Liquidity.Application.Interfaces;

public interface ILiquiditySnapshotRepository
{
    Task SaveAsync(LiquiditySnapshot snapshot, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LiquiditySnapshot>> GetAllAsync(CancellationToken cancellationToken);
}
