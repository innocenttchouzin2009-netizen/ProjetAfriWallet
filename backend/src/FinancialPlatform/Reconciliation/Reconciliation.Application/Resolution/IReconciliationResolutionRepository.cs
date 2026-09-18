using Reconciliation.Domain.Resolutions;

namespace Reconciliation.Application.Resolution;

public interface IReconciliationResolutionRepository
{
    Task<ReconciliationResolution?> GetByReviewIdAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ReconciliationResolution resolution,
        CancellationToken cancellationToken = default);
}
