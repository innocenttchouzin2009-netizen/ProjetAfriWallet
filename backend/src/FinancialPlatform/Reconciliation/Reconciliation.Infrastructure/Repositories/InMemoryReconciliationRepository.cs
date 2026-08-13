using System.Collections.Concurrent;
using Reconciliation.Application.Interfaces;
using Reconciliation.Domain.Runs;

namespace Reconciliation.Infrastructure.Repositories;

public sealed class InMemoryReconciliationRepository :
    IReconciliationRepository
{
    private readonly ConcurrentDictionary<
        Guid,
        ReconciliationRun> _runs = new();

    public Task AddRunAsync(
        ReconciliationRun run,
        CancellationToken cancellationToken)
    {
        if (!_runs.TryAdd(run.RunId, run))
            throw new InvalidOperationException(
                "Reconciliation run already exists.");

        return Task.CompletedTask;
    }

    public Task<ReconciliationRun?> GetRunAsync(
        Guid runId,
        CancellationToken cancellationToken)
    {
        _runs.TryGetValue(runId, out var run);
        return Task.FromResult(run);
    }
}
