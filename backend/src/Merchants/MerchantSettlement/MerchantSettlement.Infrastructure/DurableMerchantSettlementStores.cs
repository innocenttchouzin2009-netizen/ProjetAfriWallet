using System.Globalization;
using System.Text.Json;
using AfriWallet.Merchants.Settlement.Application.Abstractions;
using AfriWallet.Merchants.Settlement.Domain.Compensation;
using AfriWallet.Merchants.Settlement.Domain.Settlements;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Merchants.Settlement.Infrastructure;

public sealed class EfMerchantCaptureSnapshotStore(MerchantSettlementDbContext db)
    : IMerchantCaptureSnapshotStore
{
    public async Task<CaptureEligibleDecisionSnapshot?> GetAsync(Guid decisionId, CancellationToken ct = default)
    {
        var row = await db.Captures.AsNoTracking().SingleOrDefaultAsync(x => x.DecisionId == decisionId, ct);
        return row is null ? null : Map(row);
    }

    public async Task SaveAsync(CaptureEligibleDecisionSnapshot snapshot, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var existing = await db.Captures.SingleOrDefaultAsync(x => x.DecisionId == snapshot.DecisionId, ct);
        if (existing is not null)
        {
            var current = Map(existing);
            if (current != snapshot)
                throw new InvalidOperationException("Capture snapshot is immutable once recorded.");
            return;
        }

        db.Captures.Add(new MerchantCaptureEntity
        {
            DecisionId = snapshot.DecisionId,
            PaymentIntentId = snapshot.PaymentIntentId,
            MerchantId = snapshot.MerchantId,
            DecisionType = snapshot.DecisionType,
            DecisionStatus = snapshot.DecisionStatus,
            AmountMinor = snapshot.AmountMinor,
            Currency = snapshot.Currency.Trim().ToUpperInvariant(),
            MerchantRegistryStatus = snapshot.MerchantRegistryStatus,
            MerchantVerificationStatus = snapshot.MerchantVerificationStatus
        });
        await db.SaveChangesAsync(ct);
    }

    private static CaptureEligibleDecisionSnapshot Map(MerchantCaptureEntity x) =>
        new(x.DecisionId, x.PaymentIntentId, x.MerchantId, x.DecisionType, x.DecisionStatus,
            x.AmountMinor, x.Currency, x.MerchantRegistryStatus, x.MerchantVerificationStatus);
}

public sealed class DurableCaptureEligibleDecisionReader(IMerchantCaptureSnapshotStore captures)
    : ICaptureEligibleDecisionReader
{
    public Task<CaptureEligibleDecisionSnapshot?> GetAsync(Guid decisionId, CancellationToken ct = default) =>
        captures.GetAsync(decisionId, ct);
}

public sealed class EfMerchantSettlementRepository(MerchantSettlementDbContext db)
    : IMerchantSettlementRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task AddAsync(MerchantSettlementOrchestration settlement, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settlement);
        if (await db.Settlements.AnyAsync(x =>
                x.SettlementId == settlement.SettlementId ||
                x.PaymentDecisionId == settlement.PaymentDecisionId ||
                x.IdempotencyKey == settlement.IdempotencyKey, ct))
            throw new InvalidOperationException("Settlement orchestration already exists.");

        db.Settlements.Add(Map(settlement));
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(MerchantSettlementOrchestration settlement, CancellationToken ct = default)
    {
        var row = await db.Settlements.SingleOrDefaultAsync(x => x.SettlementId == settlement.SettlementId, ct)
            ?? throw new KeyNotFoundException("Merchant settlement not found.");
        Apply(settlement, row);
        await db.SaveChangesAsync(ct);
    }

    public async Task<MerchantSettlementOrchestration?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var row = await db.Settlements.AsNoTracking().SingleOrDefaultAsync(x => x.SettlementId == id, ct);
        return row is null ? null : Restore(row);
    }

    public async Task<MerchantSettlementOrchestration?> GetByDecisionAsync(Guid id, CancellationToken ct = default)
    {
        var row = await db.Settlements.AsNoTracking().SingleOrDefaultAsync(x => x.PaymentDecisionId == id, ct);
        return row is null ? null : Restore(row);
    }

    public async Task<MerchantSettlementOrchestration?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default)
    {
        var normalized = key?.Trim() ?? string.Empty;
        var row = await db.Settlements.AsNoTracking().SingleOrDefaultAsync(x => x.IdempotencyKey == normalized, ct);
        return row is null ? null : Restore(row);
    }

    private static MerchantSettlementEntity Map(MerchantSettlementOrchestration x)
    {
        var row = new MerchantSettlementEntity { SettlementId = x.SettlementId };
        Apply(x, row);
        return row;
    }

    private static void Apply(MerchantSettlementOrchestration x, MerchantSettlementEntity row)
    {
        row.PaymentDecisionId = x.PaymentDecisionId;
        row.PaymentIntentId = x.PaymentIntentId;
        row.MerchantId = x.MerchantId;
        row.Route = (int)x.Route;
        row.AmountMinor = x.AmountMinor;
        row.Currency = x.Currency;
        row.IdempotencyKey = x.IdempotencyKey;
        row.Status = (int)x.Status;
        row.ReasonCode = (int)x.ReasonCode;
        row.CorrelationId = x.CorrelationId;
        row.ProviderReference = x.ProviderReference;
        row.AttemptsJson = JsonSerializer.Serialize(x.Attempts, Json);
        row.CompensationsJson = JsonSerializer.Serialize(x.Compensations, Json);
        row.CreatedAtUtc = Format(x.CreatedAtUtc);
        row.UpdatedAtUtc = Format(x.UpdatedAtUtc);
        row.CompletedAtUtc = x.CompletedAtUtc is null ? null : Format(x.CompletedAtUtc.Value);
    }

    private static MerchantSettlementOrchestration Restore(MerchantSettlementEntity x) =>
        MerchantSettlementOrchestration.Restore(
            x.SettlementId, x.PaymentDecisionId, x.PaymentIntentId, x.MerchantId,
            (MerchantSettlementRoute)x.Route, x.AmountMinor, x.Currency, x.IdempotencyKey,
            (MerchantSettlementStatus)x.Status, (MerchantSettlementReasonCode)x.ReasonCode,
            x.CorrelationId, x.ProviderReference,
            JsonSerializer.Deserialize<MerchantSettlementAttempt[]>(x.AttemptsJson, Json) ?? [],
            JsonSerializer.Deserialize<MerchantSettlementCompensation[]>(x.CompensationsJson, Json) ?? [],
            Parse(x.CreatedAtUtc), Parse(x.UpdatedAtUtc),
            x.CompletedAtUtc is null ? null : Parse(x.CompletedAtUtc));

    private static string Format(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);
    private static DateTimeOffset Parse(string value) =>
        DateTimeOffset.ParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}

public sealed class EfMerchantSettlementAuditStore(MerchantSettlementDbContext db)
    : IMerchantSettlementAuditStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task AppendAsync(MerchantSettlementAuditEvent e, CancellationToken ct = default)
    {
        db.AuditEvents.Add(new MerchantSettlementAuditEntity
        {
            EventId = e.EventId,
            SettlementId = e.SettlementId,
            PaymentDecisionId = e.PaymentDecisionId,
            MerchantId = e.MerchantId,
            EventType = e.EventType,
            Actor = e.Actor,
            OccurredAtUtc = e.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture),
            MetadataJson = JsonSerializer.Serialize(e.Metadata, Json)
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyCollection<MerchantSettlementAuditEvent>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var rows = await db.AuditEvents.AsNoTracking()
            .Where(x => x.SettlementId == id)
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.EventId)
            .ToListAsync(ct);

        return rows.Select(x => new MerchantSettlementAuditEvent(
            x.EventId, x.SettlementId, x.PaymentDecisionId, x.MerchantId, x.EventType, x.Actor,
            DateTimeOffset.ParseExact(x.OccurredAtUtc, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            JsonSerializer.Deserialize<Dictionary<string,string>>(x.MetadataJson, Json)
                ?? new Dictionary<string,string>())).ToArray();
    }
}
