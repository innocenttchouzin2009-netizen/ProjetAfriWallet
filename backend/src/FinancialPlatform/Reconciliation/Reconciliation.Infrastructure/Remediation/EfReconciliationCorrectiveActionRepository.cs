using Microsoft.EntityFrameworkCore;
using Reconciliation.Application.Remediation;
using Reconciliation.Domain.Remediation;

namespace Reconciliation.Infrastructure.Remediation;

public sealed class EfReconciliationCorrectiveActionRepository(
    ReconciliationCorrectiveActionDbContext dbContext)
    : IReconciliationCorrectiveActionRepository, IReconciliationCorrectiveActionAuditReader
{
    public async Task<ReconciliationCorrectiveAction?> GetByResolutionIdAsync(
        Guid resolutionId,
        CancellationToken cancellationToken = default)
    {
        if (resolutionId == Guid.Empty)
            throw new ArgumentException("Resolution id is required.", nameof(resolutionId));

        cancellationToken.ThrowIfCancellationRequested();

        var entity = await dbContext.Actions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ResolutionId == resolutionId, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task AddAsync(
        ReconciliationCorrectiveAction action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.Actions.Add(ToEntity(action));
        dbContext.Audit.Add(CreateAudit(
            action,
            ReconciliationCorrectiveActionAuditEvent.Created,
            action.CreatedBy,
            null,
            null,
            action.CreatedAtUtc));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new InvalidOperationException(
                "Resolution already has a durable reconciliation corrective action.",
                exception);
        }
    }

    public async Task UpdateAsync(
        ReconciliationCorrectiveAction action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var entity = await dbContext.Actions
            .SingleOrDefaultAsync(x => x.ActionId == action.ActionId, cancellationToken);

        if (entity is null)
            throw new InvalidOperationException("Corrective action was not found for update.");

        if (entity.ResolutionId != action.ResolutionId)
            throw new InvalidOperationException("Corrective action resolution binding cannot change.");

        var persistedStatus = (ReconciliationCorrectiveActionStatus)entity.Status;
        if (persistedStatus != ReconciliationCorrectiveActionStatus.Pending)
            throw new InvalidOperationException("Persisted corrective action is already terminal.");

        if (action.Status == ReconciliationCorrectiveActionStatus.Pending)
            throw new InvalidOperationException("Corrective action update requires a terminal lifecycle transition.");

        ReconciliationCorrectiveActionAuditEntity audit;

        switch (action.Status)
        {
            case ReconciliationCorrectiveActionStatus.Completed:
                audit = CreateAudit(
                    action,
                    ReconciliationCorrectiveActionAuditEvent.Completed,
                    action.CompletedBy!,
                    action.CompletionEvidenceReference,
                    null,
                    action.CompletedAtUtc!.Value);
                break;

            case ReconciliationCorrectiveActionStatus.Cancelled:
                audit = CreateAudit(
                    action,
                    ReconciliationCorrectiveActionAuditEvent.Cancelled,
                    action.CancelledBy!,
                    null,
                    action.CancellationReason,
                    action.CancelledAtUtc!.Value);
                break;

            default:
                throw new InvalidOperationException("Unsupported corrective action lifecycle transition.");
        }

        ApplyLifecycle(entity, action);
        dbContext.Audit.Add(audit);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReconciliationCorrectiveActionAuditEntry>> ListByResolutionIdAsync(
        Guid resolutionId,
        CancellationToken cancellationToken = default)
    {
        if (resolutionId == Guid.Empty)
            throw new ArgumentException("Resolution id is required.", nameof(resolutionId));

        cancellationToken.ThrowIfCancellationRequested();

        var entries = await dbContext.Audit
            .AsNoTracking()
            .Where(x => x.ResolutionId == resolutionId)
            .OrderBy(x => x.RecordedAtUtc)
            .ThenBy(x => x.AuditId)
            .ToArrayAsync(cancellationToken);

        return entries.Select(x => new ReconciliationCorrectiveActionAuditEntry(
            x.AuditId,
            x.ActionId,
            x.ResolutionId,
            (ReconciliationCorrectiveActionAuditEvent)x.Event,
            (ReconciliationCorrectiveActionStatus)x.Status,
            x.ActorId,
            x.EvidenceReference,
            x.CancellationReason,
            DateTime.SpecifyKind(x.RecordedAtUtc, DateTimeKind.Utc))).ToArray();
    }

    private static ReconciliationCorrectiveActionEntity ToEntity(ReconciliationCorrectiveAction action) => new()
    {
        ActionId = action.ActionId,
        ResolutionId = action.ResolutionId,
        ReviewId = action.ReviewId,
        PartnerId = action.PartnerId,
        InternalRecordId = action.InternalRecordId,
        ExternalRecordId = action.ExternalRecordId,
        ActionCode = action.ActionCode,
        Description = action.Description,
        CreatedBy = action.CreatedBy,
        CreatedAtUtc = action.CreatedAtUtc,
        Status = (int)action.Status,
        CompletedBy = action.CompletedBy,
        CompletionEvidenceReference = action.CompletionEvidenceReference,
        CompletedAtUtc = action.CompletedAtUtc,
        CancelledBy = action.CancelledBy,
        CancellationReason = action.CancellationReason,
        CancelledAtUtc = action.CancelledAtUtc
    };

    private static void ApplyLifecycle(
        ReconciliationCorrectiveActionEntity entity,
        ReconciliationCorrectiveAction action)
    {
        entity.Status = (int)action.Status;
        entity.CompletedBy = action.CompletedBy;
        entity.CompletionEvidenceReference = action.CompletionEvidenceReference;
        entity.CompletedAtUtc = action.CompletedAtUtc;
        entity.CancelledBy = action.CancelledBy;
        entity.CancellationReason = action.CancellationReason;
        entity.CancelledAtUtc = action.CancelledAtUtc;
    }

    private static ReconciliationCorrectiveActionAuditEntity CreateAudit(
        ReconciliationCorrectiveAction action,
        ReconciliationCorrectiveActionAuditEvent auditEvent,
        string actorId,
        string? evidenceReference,
        string? cancellationReason,
        DateTime recordedAtUtc) => new()
    {
        AuditId = Guid.NewGuid(),
        ActionId = action.ActionId,
        ResolutionId = action.ResolutionId,
        Event = (int)auditEvent,
        Status = (int)action.Status,
        ActorId = actorId,
        EvidenceReference = evidenceReference,
        CancellationReason = cancellationReason,
        RecordedAtUtc = recordedAtUtc
    };

    private static ReconciliationCorrectiveAction ToDomain(
        ReconciliationCorrectiveActionEntity entity) =>
        ReconciliationCorrectiveAction.Restore(
            entity.ActionId,
            entity.ResolutionId,
            entity.ReviewId,
            entity.PartnerId,
            entity.InternalRecordId,
            entity.ExternalRecordId,
            entity.ActionCode,
            entity.Description,
            entity.CreatedBy,
            DateTime.SpecifyKind(entity.CreatedAtUtc, DateTimeKind.Utc),
            (ReconciliationCorrectiveActionStatus)entity.Status,
            entity.CompletedBy,
            entity.CompletionEvidenceReference,
            entity.CompletedAtUtc is null
                ? null
                : DateTime.SpecifyKind(entity.CompletedAtUtc.Value, DateTimeKind.Utc),
            entity.CancelledBy,
            entity.CancellationReason,
            entity.CancelledAtUtc is null
                ? null
                : DateTime.SpecifyKind(entity.CancelledAtUtc.Value, DateTimeKind.Utc));
}
