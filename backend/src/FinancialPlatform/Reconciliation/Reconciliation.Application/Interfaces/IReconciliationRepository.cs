using Reconciliation.Domain.Runs;

namespace Reconciliation.Application.Interfaces;

public interface IReconciliationRepository
{
    Task AddRunAsync(
        ReconciliationRun run,
        CancellationToken cancellationToken);

    Task<ReconciliationRun?> GetRunAsync(
        Guid runId,
        CancellationToken cancellationToken);
}
