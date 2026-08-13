using Liquidity.Application.Interfaces;
using Liquidity.Domain.Snapshots;

namespace Liquidity.Infrastructure.Repositories;

public sealed class InMemoryLiquiditySnapshotRepository : ILiquiditySnapshotRepository
{
    private readonly List<LiquiditySnapshot> _snapshots = [];

    public Task SaveAsync(LiquiditySnapshot snapshot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _snapshots.Add(snapshot);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<LiquiditySnapshot>> GetAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<LiquiditySnapshot>>(_snapshots.OrderByDescending(x => x.CreatedUtc).ToArray());
    }
}
