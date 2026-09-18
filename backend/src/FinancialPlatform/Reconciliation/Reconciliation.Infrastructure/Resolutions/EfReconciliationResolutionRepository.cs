using Microsoft.EntityFrameworkCore;
using Reconciliation.Application.Resolution;
using Reconciliation.Domain.Resolutions;

namespace Reconciliation.Infrastructure.Resolutions;

public sealed class EfReconciliationResolutionRepository(ReconciliationResolutionDbContext dbContext)
    : IReconciliationResolutionRepository, IReconciliationResolutionAuditReader
{
    public async Task<ReconciliationResolution?> GetByReviewIdAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await dbContext.Resolutions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ReviewId == reviewId, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<ReconciliationResolution>> ListAsync(
        ReconciliationResolutionRepositoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        IQueryable<ReconciliationResolutionEntity> source = dbContext.Resolutions.AsNoTracking();

        if (query.PartnerIds is { Count: > 0 })
        {
            var partners = query.PartnerIds.ToArray();
            source = source.Where(x => partners.Contains(x.PartnerId));
        }

        if (query.Disposition is not null)
        {
            var disposition = (int)query.Disposition.Value;
            source = source.Where(x => x.Disposition == disposition);
        }

        if (!string.IsNullOrWhiteSpace(query.ResolvedBy))
            source = source.Where(x => x.ResolvedBy == query.ResolvedBy);

        if (query.ResolvedFromUtc is not null)
            source = source.Where(x => x.ResolvedAtUtc >= query.ResolvedFromUtc.Value);

        if (query.ResolvedToUtc is not null)
            source = source.Where(x => x.ResolvedAtUtc <= query.ResolvedToUtc.Value);

        var items = await source
            .OrderByDescending(x => x.ResolvedAtUtc)
            .ThenBy(x => x.ResolutionId)
            .Take(query.Limit)
            .ToArrayAsync(cancellationToken);

        return items.Select(ToDomain).ToArray();
    }

    public async Task AddAsync(
        ReconciliationResolution resolution,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        cancellationToken.ThrowIfCancellationRequested();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.Resolutions.Add(ToEntity(resolution));
        dbContext.Audit.Add(new ReconciliationResolutionAuditEntity
        {
            AuditId = Guid.NewGuid(),
            ResolutionId = resolution.ResolutionId,
            ReviewId = resolution.ReviewId,
            Disposition = (int)resolution.Disposition,
            ResolvedBy = resolution.ResolvedBy,
            Rationale = resolution.Rationale,
            EvidenceReference = resolution.EvidenceReference,
            RecordedAtUtc = resolution.ResolvedAtUtc
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new InvalidOperationException(
                "Review already has a durable reconciliation resolution.",
                exception);
        }
    }

    public async Task<IReadOnlyList<ReconciliationResolutionAuditEntry>> ListByReviewIdAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        if (reviewId == Guid.Empty)
            throw new ArgumentException("Review id is required.", nameof(reviewId));

        cancellationToken.ThrowIfCancellationRequested();

        var entries = await dbContext.Audit
            .AsNoTracking()
            .Where(x => x.ReviewId == reviewId)
            .OrderBy(x => x.RecordedAtUtc)
            .ThenBy(x => x.AuditId)
            .ToArrayAsync(cancellationToken);

        return entries.Select(x => new ReconciliationResolutionAuditEntry(
            x.AuditId,
            x.ResolutionId,
            x.ReviewId,
            (ReconciliationResolutionDisposition)x.Disposition,
            x.ResolvedBy,
            x.Rationale,
            x.EvidenceReference,
            DateTime.SpecifyKind(x.RecordedAtUtc, DateTimeKind.Utc))).ToArray();
    }

    private static ReconciliationResolutionEntity ToEntity(ReconciliationResolution resolution) => new()
    {
        ResolutionId = resolution.ResolutionId,
        ReviewId = resolution.ReviewId,
        PartnerId = resolution.PartnerId,
        InternalRecordId = resolution.InternalRecordId,
        ExternalRecordId = resolution.ExternalRecordId,
        Disposition = (int)resolution.Disposition,
        Rationale = resolution.Rationale,
        EvidenceReference = resolution.EvidenceReference,
        ResolvedBy = resolution.ResolvedBy,
        ResolvedAtUtc = resolution.ResolvedAtUtc
    };

    private static ReconciliationResolution ToDomain(ReconciliationResolutionEntity entity) =>
        ReconciliationResolution.Restore(
            entity.ResolutionId,
            entity.ReviewId,
            entity.PartnerId,
            entity.InternalRecordId,
            entity.ExternalRecordId,
            (ReconciliationResolutionDisposition)entity.Disposition,
            entity.Rationale,
            entity.EvidenceReference,
            entity.ResolvedBy,
            DateTime.SpecifyKind(entity.ResolvedAtUtc, DateTimeKind.Utc));
}
