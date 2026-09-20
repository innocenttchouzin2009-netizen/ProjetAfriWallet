using Microsoft.EntityFrameworkCore;
using Reconciliation.Application.Review;
using Reconciliation.Domain.Matches;

namespace Reconciliation.Infrastructure.Repositories;

public sealed class EfReconciliationReviewRepository(ReconciliationReviewDbContext dbContext)
    : IReconciliationReviewRepository
{
    public async Task AddQueueAsync(
        ReconciliationReviewQueue queue,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queue);
        cancellationToken.ThrowIfCancellationRequested();

        var entities = queue.Items.Select(ToEntity).ToArray();
        dbContext.ReviewItems.AddRange(entities);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new InvalidOperationException("One or more reconciliation review items already exist.", exception);
        }
    }

    public async Task<ReconciliationReviewItem?> GetAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entity = await dbContext.ReviewItems
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ReviewId == reviewId, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    public async Task<IReadOnlyList<ReconciliationReviewItem>> ListAsync(
        string partnerId,
        ReconciliationReviewStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = dbContext.ReviewItems
            .AsNoTracking()
            .Where(x => x.PartnerId == partnerId);

        if (status is not null)
        {
            var statusValue = (int)status.Value;
            query = query.Where(x => x.Status == statusValue);
        }

        var entities = await query
            .OrderBy(x => x.QueuedAtUtc)
            .ThenBy(x => x.ReviewId)
            .ToArrayAsync(cancellationToken);

        return entities.Select(ToModel).ToArray();
    }

    public async Task<bool> TryReplaceAsync(
        ReconciliationReviewItem item,
        ReconciliationReviewStatus expectedStatus,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        cancellationToken.ThrowIfCancellationRequested();

        if (item.Status == expectedStatus)
            throw new ArgumentException("Replacement status must differ from the expected status.", nameof(item));
        if (item.ReviewerId is null || item.DecidedAtUtc is null)
            throw new ArgumentException("A stored review decision requires reviewer and decision timestamp.", nameof(item));

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var affected = await dbContext.ReviewItems
            .Where(x => x.ReviewId == item.ReviewId && x.Status == (int)expectedStatus)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, (int)item.Status)
                .SetProperty(x => x.ReviewerId, item.ReviewerId)
                .SetProperty(x => x.DecisionReason, item.DecisionReason)
                .SetProperty(x => x.DecidedAtUtc, item.DecidedAtUtc), cancellationToken);

        if (affected != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        dbContext.ReviewAudit.Add(new ReconciliationReviewAuditEntity
        {
            AuditId = Guid.NewGuid(),
            ReviewId = item.ReviewId,
            PreviousStatus = (int)expectedStatus,
            NewStatus = (int)item.Status,
            ReviewerId = item.ReviewerId,
            Reason = item.DecisionReason,
            DecidedAtUtc = item.DecidedAtUtc.Value
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static ReconciliationReviewEntity ToEntity(ReconciliationReviewItem item) => new()
    {
        ReviewId = item.ReviewId,
        PartnerId = item.PartnerId,
        InternalRecordId = item.InternalRecordId,
        ExternalRecordId = item.ExternalRecordId,
        MatchType = (int)item.MatchType,
        ConfidenceScore = item.ConfidenceScore,
        AmountDifferenceMinor = item.AmountDifferenceMinor,
        TimeDifferenceTicks = item.TimeDifference?.Ticks,
        QueuedAtUtc = item.QueuedAtUtc,
        Status = (int)item.Status,
        ReviewerId = item.ReviewerId,
        DecisionReason = item.DecisionReason,
        DecidedAtUtc = item.DecidedAtUtc
    };

    private static ReconciliationReviewItem ToModel(ReconciliationReviewEntity entity) => new(
        entity.ReviewId,
        entity.PartnerId,
        entity.InternalRecordId,
        entity.ExternalRecordId,
        (ReconciliationMatchType)entity.MatchType,
        entity.ConfidenceScore,
        entity.AmountDifferenceMinor,
        entity.TimeDifferenceTicks is null ? null : TimeSpan.FromTicks(entity.TimeDifferenceTicks.Value),
        DateTime.SpecifyKind(entity.QueuedAtUtc, DateTimeKind.Utc),
        (ReconciliationReviewStatus)entity.Status,
        entity.ReviewerId,
        entity.DecisionReason,
        entity.DecidedAtUtc is null ? null : DateTime.SpecifyKind(entity.DecidedAtUtc.Value, DateTimeKind.Utc));
}
