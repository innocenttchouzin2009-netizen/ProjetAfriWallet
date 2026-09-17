using Microsoft.EntityFrameworkCore;
using Reconciliation.Application.Review.Resolution;
using Reconciliation.Domain.ReviewResolution;

namespace Reconciliation.Infrastructure.Repositories;

public sealed class EfReviewResolutionStore(ReconciliationReviewDbContext dbContext)
    : IReviewResolutionStore
{
    public async Task<ReviewResolutionRecord?> GetAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (reviewId == Guid.Empty)
            throw new ArgumentException("Review id is required.", nameof(reviewId));

        var entity = await dbContext.ReviewResolutions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ReviewId == reviewId, cancellationToken);

        return entity is null ? null : ToModel(entity);
    }

    public async Task<bool> TryAddAsync(
        ReviewResolutionRecord resolution,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        cancellationToken.ThrowIfCancellationRequested();
        resolution = resolution.Validate();

        var entity = new ReviewResolutionEntity
        {
            ReviewId = resolution.ReviewId,
            PartnerId = resolution.PartnerId,
            EvidenceId = resolution.EvidenceId,
            ResolvedBy = resolution.ResolvedBy,
            ResolvedAtUtc = resolution.ResolvedAtUtc
        };

        dbContext.ReviewResolutions.Add(entity);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            dbContext.Entry(entity).State = EntityState.Detached;
            return true;
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(entity).State = EntityState.Detached;
            return false;
        }
    }

    private static ReviewResolutionRecord ToModel(ReviewResolutionEntity entity) =>
        new ReviewResolutionRecord(
            entity.ReviewId,
            entity.PartnerId,
            entity.EvidenceId,
            entity.ResolvedBy,
            DateTime.SpecifyKind(entity.ResolvedAtUtc, DateTimeKind.Utc)).Validate();
}
