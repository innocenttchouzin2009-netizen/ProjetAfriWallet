using System.Text.Json;
using AfriWallet.Merchants.Billing.Application;
using AfriWallet.Merchants.Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Merchants.Billing.Infrastructure;

public sealed class EfMerchantBillingHandoffRepository(MerchantBillingDbContext db)
    : IMerchantBillingHandoffRepository
{
    public async Task AddAsync(MerchantBillingHandoff handoff, CancellationToken cancellationToken = default)
    {
        if (await db.Handoffs.AnyAsync(x => x.CaptureExecutionId == handoff.CaptureExecutionId, cancellationToken))
            throw new InvalidOperationException("Merchant billing handoff already exists for this capture.");

        db.Handoffs.Add(Map(handoff));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveAsync(MerchantBillingHandoff handoff, CancellationToken cancellationToken = default)
    {
        var row = await db.Handoffs.SingleAsync(x => x.HandoffId == handoff.HandoffId, cancellationToken);
        Copy(handoff, row);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<MerchantBillingHandoff?> GetAsync(Guid handoffId, CancellationToken cancellationToken = default)
    {
        var row = await db.Handoffs.AsNoTracking().SingleOrDefaultAsync(x => x.HandoffId == handoffId, cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<MerchantBillingHandoff?> GetByCaptureAsync(Guid captureExecutionId, CancellationToken cancellationToken = default)
    {
        var row = await db.Handoffs.AsNoTracking().SingleOrDefaultAsync(x => x.CaptureExecutionId == captureExecutionId, cancellationToken);
        return row is null ? null : Map(row);
    }

    private static MerchantBillingHandoffEntity Map(MerchantBillingHandoff value)
    {
        var row = new MerchantBillingHandoffEntity { HandoffId = value.HandoffId };
        Copy(value, row);
        return row;
    }

    private static void Copy(MerchantBillingHandoff value, MerchantBillingHandoffEntity row)
    {
        row.CaptureExecutionId = value.CaptureExecutionId;
        row.MerchantId = value.MerchantId;
        row.Status = (int)value.Status;
        row.ReceivableId = value.ReceivableId;
        row.AttemptCount = value.AttemptCount;
        row.LastError = value.LastError;
        row.CreatedAtUtc = value.CreatedAtUtc;
        row.UpdatedAtUtc = value.UpdatedAtUtc;
    }

    private static MerchantBillingHandoff Map(MerchantBillingHandoffEntity row) =>
        MerchantBillingHandoff.Restore(
            row.HandoffId,
            row.CaptureExecutionId,
            row.MerchantId,
            (MerchantBillingHandoffStatus)row.Status,
            row.ReceivableId,
            row.AttemptCount,
            row.LastError,
            row.CreatedAtUtc,
            row.UpdatedAtUtc);
}

public sealed class EfMerchantBillingAuditStore(MerchantBillingDbContext db)
    : IMerchantBillingAuditStore
{
    public async Task AppendAsync(MerchantBillingAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        db.Audit.Add(new MerchantBillingAuditEntity
        {
            EventId = auditEvent.EventId,
            HandoffId = auditEvent.HandoffId,
            CaptureExecutionId = auditEvent.CaptureExecutionId,
            MerchantId = auditEvent.MerchantId,
            EventType = auditEvent.EventType,
            Actor = auditEvent.Actor,
            OccurredAtUtc = auditEvent.OccurredAtUtc,
            MetadataJson = JsonSerializer.Serialize(auditEvent.Metadata)
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<MerchantBillingAuditEvent>> ListAsync(
        Guid handoffId,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.Audit.AsNoTracking()
            .Where(x => x.HandoffId == handoffId)
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.EventId)
            .Select(x => new MerchantBillingAuditEvent(
            x.EventId,
            x.HandoffId,
            x.CaptureExecutionId,
            x.MerchantId,
            x.EventType,
            x.Actor,
            x.OccurredAtUtc,
            JsonSerializer.Deserialize<Dictionary<string,string>>(x.MetadataJson)
                ?? new Dictionary<string,string>()))
            .ToArray();
    }
}
