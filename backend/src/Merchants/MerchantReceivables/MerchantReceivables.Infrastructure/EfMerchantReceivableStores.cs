using System.Text.Json;
using AfriWallet.Merchants.Receivables.Application;
using AfriWallet.Merchants.Receivables.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Merchants.Receivables.Infrastructure;

public sealed class EfMerchantFeeScheduleStore(MerchantReceivablesDbContext db) : IMerchantFeeScheduleStore
{
    public async Task UpsertAsync(MerchantFeeSchedule schedule, CancellationToken cancellationToken = default)
    {
        var row = await db.FeeSchedules.SingleOrDefaultAsync(
            x => x.MerchantId == schedule.MerchantId && x.Currency == schedule.Currency,
            cancellationToken);

        if (row is null)
        {
            db.FeeSchedules.Add(new MerchantFeeScheduleEntity
            {
                MerchantId = schedule.MerchantId,
                Currency = schedule.Currency,
                PercentageBasisPoints = schedule.PercentageBasisPoints,
                FixedFeeMinor = schedule.FixedFeeMinor
            });
        }
        else
        {
            row.PercentageBasisPoints = schedule.PercentageBasisPoints;
            row.FixedFeeMinor = schedule.FixedFeeMinor;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<MerchantFeeSchedule?> GetAsync(string merchantId, string currency, CancellationToken cancellationToken = default)
    {
        var normalizedMerchant = merchantId.Trim().ToUpperInvariant();
        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        var row = await db.FeeSchedules.AsNoTracking().SingleOrDefaultAsync(
            x => x.MerchantId == normalizedMerchant && x.Currency == normalizedCurrency,
            cancellationToken);

        return row is null ? null : new MerchantFeeSchedule(
            row.MerchantId,
            row.Currency,
            row.PercentageBasisPoints,
            row.FixedFeeMinor);
    }
}

public sealed class EfMerchantReceivableRepository(MerchantReceivablesDbContext db) : IMerchantReceivableRepository
{
    public async Task AddAsync(MerchantReceivable value, CancellationToken cancellationToken = default)
    {
        if (await db.Receivables.AnyAsync(
                x => x.CaptureExecutionId == value.CaptureExecutionId || x.IdempotencyKey == value.IdempotencyKey,
                cancellationToken))
            throw new InvalidOperationException("Merchant receivable already exists.");

        db.Receivables.Add(Map(value));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveAsync(MerchantReceivable value, CancellationToken cancellationToken = default)
    {
        var row = await db.Receivables.SingleAsync(x => x.ReceivableId == value.ReceivableId, cancellationToken);
        Copy(value, row);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<MerchantReceivable?> GetAsync(Guid receivableId, CancellationToken cancellationToken = default)
    {
        var row = await db.Receivables.AsNoTracking().SingleOrDefaultAsync(x => x.ReceivableId == receivableId, cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<MerchantReceivable?> GetByCaptureAsync(Guid captureExecutionId, CancellationToken cancellationToken = default)
    {
        var row = await db.Receivables.AsNoTracking().SingleOrDefaultAsync(x => x.CaptureExecutionId == captureExecutionId, cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<MerchantReceivable?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var normalized = idempotencyKey.Trim();
        var row = await db.Receivables.AsNoTracking().SingleOrDefaultAsync(x => x.IdempotencyKey == normalized, cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyCollection<MerchantReceivable>> ListOpenAsync(
        string merchantId,
        string currency,
        CancellationToken cancellationToken = default)
    {
        var m = merchantId.Trim().ToUpperInvariant();
        var c = currency.Trim().ToUpperInvariant();
        var rows = await db.Receivables.AsNoTracking()
            .Where(x => x.MerchantId == m && x.Currency == c && x.Status == (int)MerchantReceivableStatus.Open)
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.ReceivableId)
            .Select(Map)
            .ToArray();
    }

    private static MerchantReceivableEntity Map(MerchantReceivable value)
    {
        var row = new MerchantReceivableEntity { ReceivableId = value.ReceivableId };
        Copy(value, row);
        return row;
    }

    private static void Copy(MerchantReceivable value, MerchantReceivableEntity row)
    {
        row.CaptureExecutionId = value.CaptureExecutionId;
        row.DecisionId = value.DecisionId;
        row.PaymentIntentId = value.PaymentIntentId;
        row.MerchantId = value.MerchantId;
        row.Currency = value.Currency;
        row.GrossAmountMinor = value.GrossAmountMinor;
        row.FeeAmountMinor = value.FeeAmountMinor;
        row.NetAmountMinor = value.NetAmountMinor;
        row.IdempotencyKey = value.IdempotencyKey;
        row.Status = (int)value.Status;
        row.SettlementId = value.SettlementId;
        row.CreatedAtUtc = value.CreatedAtUtc;
        row.UpdatedAtUtc = value.UpdatedAtUtc;
        row.SettledAtUtc = value.SettledAtUtc;
    }

    private static MerchantReceivable Map(MerchantReceivableEntity row) =>
        MerchantReceivable.Restore(
            row.ReceivableId,
            row.CaptureExecutionId,
            row.DecisionId,
            row.PaymentIntentId,
            row.MerchantId,
            row.Currency,
            row.GrossAmountMinor,
            row.FeeAmountMinor,
            row.NetAmountMinor,
            row.IdempotencyKey,
            (MerchantReceivableStatus)row.Status,
            row.SettlementId,
            row.CreatedAtUtc,
            row.UpdatedAtUtc,
            row.SettledAtUtc);
}

public sealed class EfMerchantReceivableAuditStore(MerchantReceivablesDbContext db) : IMerchantReceivableAuditStore
{
    public async Task AppendAsync(MerchantReceivableAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        db.Audit.Add(new MerchantReceivableAuditEntity
        {
            EventId = auditEvent.EventId,
            ReceivableId = auditEvent.ReceivableId,
            CaptureExecutionId = auditEvent.CaptureExecutionId,
            MerchantId = auditEvent.MerchantId,
            EventType = auditEvent.EventType,
            Actor = auditEvent.Actor,
            OccurredAtUtc = auditEvent.OccurredAtUtc,
            MetadataJson = JsonSerializer.Serialize(auditEvent.Metadata)
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<MerchantReceivableAuditEvent>> ListAsync(Guid receivableId, CancellationToken cancellationToken = default)
    {
        var rows = await db.Audit.AsNoTracking()
            .Where(x => x.ReceivableId == receivableId)
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.EventId)
            .Select(x => new MerchantReceivableAuditEvent(
            x.EventId,
            x.ReceivableId,
            x.CaptureExecutionId,
            x.MerchantId,
            x.EventType,
            x.Actor,
            x.OccurredAtUtc,
            JsonSerializer.Deserialize<Dictionary<string,string>>(x.MetadataJson) ?? new Dictionary<string,string>()))
            .ToArray();
    }
}
