using System.Text.Json;
using AfriWallet.Merchants.Payout.Application;
using AfriWallet.Merchants.Payout.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Merchants.Payout.Infrastructure;

public sealed class EfMerchantReceivableStore(MerchantPayoutDbContext db) : IMerchantReceivableStore
{
    public async Task SaveAsync(MerchantReceivableSnapshot receivable, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(receivable);
        var existing = await db.Receivables.SingleOrDefaultAsync(x => x.ReceivableId == receivable.ReceivableId, cancellationToken);
        if (existing is not null)
        {
            var current = Map(existing);
            if (current != receivable)
                throw new InvalidOperationException("Merchant receivable is immutable once recorded.");
            return;
        }

        db.Receivables.Add(new MerchantReceivableEntity
        {
            ReceivableId = receivable.ReceivableId,
            MerchantId = receivable.MerchantId.Trim(),
            AmountMinor = receivable.AmountMinor,
            Currency = receivable.Currency.Trim().ToUpperInvariant(),
            CaptureStatus = receivable.CaptureStatus.Trim(),
            SettlementReady = receivable.SettlementReady,
            CaptureProviderReference = receivable.CaptureProviderReference
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<MerchantReceivableSnapshot?> GetAsync(Guid receivableId, CancellationToken cancellationToken = default)
    {
        var row = await db.Receivables.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ReceivableId == receivableId, cancellationToken);
        return row is null ? null : Map(row);
    }

    private static MerchantReceivableSnapshot Map(MerchantReceivableEntity row) =>
        new(row.ReceivableId, row.MerchantId, row.AmountMinor, row.Currency, row.CaptureStatus, row.SettlementReady, row.CaptureProviderReference);
}

public sealed class EfMerchantPayoutDestinationRepository(MerchantPayoutDbContext db) : IMerchantPayoutDestinationRepository
{
    public async Task AddAsync(MerchantPayoutDestination destination, CancellationToken cancellationToken = default)
    {
        db.Destinations.Add(Map(destination));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveAsync(MerchantPayoutDestination destination, CancellationToken cancellationToken = default)
    {
        var row = await db.Destinations.SingleOrDefaultAsync(x => x.DestinationId == destination.DestinationId, cancellationToken)
            ?? throw new KeyNotFoundException("Merchant payout destination not found.");
        Apply(destination, row);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<MerchantPayoutDestination?> GetAsync(Guid destinationId, CancellationToken cancellationToken = default)
    {
        var row = await db.Destinations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.DestinationId == destinationId, cancellationToken);
        return row is null ? null : Map(row);
    }

    private static MerchantPayoutDestinationEntity Map(MerchantPayoutDestination value)
    {
        var row = new MerchantPayoutDestinationEntity { DestinationId = value.DestinationId };
        Apply(value, row);
        return row;
    }

    private static void Apply(MerchantPayoutDestination value, MerchantPayoutDestinationEntity row)
    {
        row.MerchantId = value.MerchantId;
        row.Type = (int)value.Type;
        row.Reference = value.Reference;
        row.Currency = value.Currency;
        row.Active = value.Active;
        row.CreatedAtUtc = value.CreatedAtUtc;
        row.UpdatedAtUtc = value.UpdatedAtUtc;
    }

    private static MerchantPayoutDestination Map(MerchantPayoutDestinationEntity row) =>
        MerchantPayoutDestination.Restore(
            row.DestinationId,
            row.MerchantId,
            (MerchantPayoutDestinationType)row.Type,
            row.Reference,
            row.Currency,
            row.Active,
            row.CreatedAtUtc,
            row.UpdatedAtUtc);
}

public sealed class EfMerchantPayoutRepository(MerchantPayoutDbContext db) : IMerchantPayoutRepository
{
    public async Task AddAsync(MerchantPayoutExecution payout, CancellationToken cancellationToken = default)
    {
        db.Payouts.Add(Map(payout));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveAsync(MerchantPayoutExecution payout, CancellationToken cancellationToken = default)
    {
        var row = await db.Payouts.SingleOrDefaultAsync(x => x.PayoutId == payout.PayoutId, cancellationToken)
            ?? throw new KeyNotFoundException("Merchant payout not found.");
        Apply(payout, row);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<MerchantPayoutExecution?> GetAsync(Guid payoutId, CancellationToken cancellationToken = default)
    {
        var row = await db.Payouts.AsNoTracking().SingleOrDefaultAsync(x => x.PayoutId == payoutId, cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<MerchantPayoutExecution?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var key = idempotencyKey?.Trim() ?? string.Empty;
        var row = await db.Payouts.AsNoTracking().SingleOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<MerchantPayoutExecution?> GetByReceivableAsync(Guid receivableId, CancellationToken cancellationToken = default)
    {
        var row = await db.Payouts.AsNoTracking().SingleOrDefaultAsync(x => x.ReceivableId == receivableId, cancellationToken);
        return row is null ? null : Map(row);
    }

    private static MerchantPayoutExecutionEntity Map(MerchantPayoutExecution value)
    {
        var row = new MerchantPayoutExecutionEntity { PayoutId = value.PayoutId };
        Apply(value, row);
        return row;
    }

    private static void Apply(MerchantPayoutExecution value, MerchantPayoutExecutionEntity row)
    {
        row.ReceivableId = value.ReceivableId;
        row.MerchantId = value.MerchantId;
        row.AmountMinor = value.AmountMinor;
        row.Currency = value.Currency;
        row.DestinationId = value.DestinationId;
        row.IdempotencyKey = value.IdempotencyKey;
        row.Status = (int)value.Status;
        row.ProviderReference = value.ProviderReference;
        row.FailureCode = value.FailureCode;
        row.CreatedAtUtc = value.CreatedAtUtc;
        row.UpdatedAtUtc = value.UpdatedAtUtc;
    }

    private static MerchantPayoutExecution Map(MerchantPayoutExecutionEntity row) =>
        MerchantPayoutExecution.Restore(
            row.PayoutId,
            row.ReceivableId,
            row.MerchantId,
            row.AmountMinor,
            row.Currency,
            row.DestinationId,
            row.IdempotencyKey,
            (MerchantPayoutStatus)row.Status,
            row.ProviderReference,
            row.FailureCode,
            row.CreatedAtUtc,
            row.UpdatedAtUtc);
}

public sealed class EfMerchantPayoutAuditStore(MerchantPayoutDbContext db) : IMerchantPayoutAuditStore
{
    public async Task AppendAsync(MerchantPayoutAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        db.Audit.Add(new MerchantPayoutAuditEntity
        {
            EventId = auditEvent.EventId,
            PayoutId = auditEvent.PayoutId,
            ReceivableId = auditEvent.ReceivableId,
            MerchantId = auditEvent.MerchantId,
            EventType = auditEvent.EventType,
            Actor = auditEvent.Actor,
            OccurredAtUtc = auditEvent.OccurredAtUtc,
            MetadataJson = JsonSerializer.Serialize(auditEvent.Metadata)
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<MerchantPayoutAuditEvent>> ListAsync(Guid payoutId, CancellationToken cancellationToken = default)
    {
        var rows = await db.Audit.AsNoTracking()
            .Where(x => x.PayoutId == payoutId)
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.EventId)
            .Select(x => new MerchantPayoutAuditEvent(
            x.EventId,
            x.PayoutId,
            x.ReceivableId,
            x.MerchantId,
            x.EventType,
            x.Actor,
            x.OccurredAtUtc,
            JsonSerializer.Deserialize<Dictionary<string,string>>(x.MetadataJson) ?? new Dictionary<string,string>()))
            .ToArray();
    }
}
