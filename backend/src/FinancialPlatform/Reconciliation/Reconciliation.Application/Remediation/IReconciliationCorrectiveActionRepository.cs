using Reconciliation.Domain.Remediation;

namespace Reconciliation.Application.Remediation;

public interface IReconciliationCorrectiveActionRepository
{
    Task<ReconciliationCorrectiveAction?> GetByResolutionIdAsync(
        Guid resolutionId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ReconciliationCorrectiveAction action,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ReconciliationCorrectiveAction action,
        CancellationToken cancellationToken = default);
}
