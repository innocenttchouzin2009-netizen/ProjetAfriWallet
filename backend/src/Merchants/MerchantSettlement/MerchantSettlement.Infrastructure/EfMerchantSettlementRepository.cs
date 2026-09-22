using AfriWallet.Merchants.Settlement.Application.Abstractions;
using AfriWallet.Merchants.Settlement.Domain.Compensation;
using AfriWallet.Merchants.Settlement.Domain.Settlements;
using AfriWallet.Merchants.Settlement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Merchants.Settlement.Infrastructure;

public sealed class EfMerchantSettlementRepository(MerchantSettlementDbContext dbContext)
    : IMerchantSettlementRepository
{
    public async Task AddAsync(MerchantSettlementOrchestration settlement, CancellationToken ct = default)
    {
        if (await dbContext.Settlements.AnyAsync(
                x => x.SettlementId == settlement.SettlementId ||
                     x.PaymentDecisionId == settlement.PaymentDecisionId ||
                     x.IdempotencyKey == settlement.IdempotencyKey, ct))
            throw new InvalidOperationException("Settlement orchestration already exists.");

        dbContext.Settlements.Add(ToEntity(settlement));
        await ReplaceChildrenAsync(settlement, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(MerchantSettlementOrchestration settlement, CancellationToken ct = default)
    {
        var entity = await dbContext.Settlements.SingleOrDefaultAsync(x => x.SettlementId == settlement.SettlementId, ct)
            ?? throw new KeyNotFoundException("Merchant settlement not found.");

        Map(settlement, entity);
        await ReplaceChildrenAsync(settlement, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<MerchantSettlementOrchestration?> GetAsync(Guid id, CancellationToken ct = default) =>
        await LoadAsync(await dbContext.Settlements.AsNoTracking().SingleOrDefaultAsync(x => x.SettlementId == id, ct), ct);

    public async Task<MerchantSettlementOrchestration?> GetByDecisionAsync(Guid id, CancellationToken ct = default) =>
        await LoadAsync(await dbContext.Settlements.AsNoTracking().SingleOrDefaultAsync(x => x.PaymentDecisionId == id, ct), ct);

    public async Task<MerchantSettlementOrchestration?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default) =>
        await LoadAsync(await dbContext.Settlements.AsNoTracking().SingleOrDefaultAsync(x => x.IdempotencyKey == key, ct), ct);

    private async Task<MerchantSettlementOrchestration?> LoadAsync(
        MerchantSettlementEntity? entity,
        CancellationToken ct)
    {
        if (entity is null) return null;

        var attempts = await dbContext.Attempts.AsNoTracking()
            .Where(x => x.SettlementId == entity.SettlementId)
            .OrderBy(x => x.AttemptNumber)
            .Select(x => new MerchantSettlementAttempt(
                x.AttemptId, x.AttemptNumber, x.CorrelationId, x.ProviderReference, x.Result, x.AttemptedAtUtc))
            .ToListAsync(ct);

        var compensations = await dbContext.Compensations.AsNoTracking()
            .Where(x => x.SettlementId == entity.SettlementId)
            .OrderBy(x => x.RequestedAtUtc)
            .Select(x => new MerchantSettlementCompensation(
                x.CompensationId, x.Reason, x.ProviderReference, x.RequestedAtUtc, x.CompletedAtUtc))
            .ToListAsync(ct);

        return MerchantSettlementOrchestration.Restore(
            entity.SettlementId,
            entity.PaymentDecisionId,
            entity.PaymentIntentId,
            entity.MerchantId,
            (MerchantSettlementRoute)entity.Route,
            entity.AmountMinor,
            entity.Currency,
            entity.IdempotencyKey,
            (MerchantSettlementStatus)entity.Status,
            (MerchantSettlementReasonCode)entity.ReasonCode,
            entity.CorrelationId,
            entity.ProviderReference,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.CompletedAtUtc,
            attempts,
            compensations);
    }

    private async Task ReplaceChildrenAsync(MerchantSettlementOrchestration settlement, CancellationToken ct)
    {
        var attemptRows = await dbContext.Attempts.Where(x => x.SettlementId == settlement.SettlementId).ToListAsync(ct);
        dbContext.Attempts.RemoveRange(attemptRows);
        dbContext.Attempts.AddRange(settlement.Attempts.Select(x => new MerchantSettlementAttemptEntity
        {
            AttemptId = x.AttemptId,
            SettlementId = settlement.SettlementId,
            AttemptNumber = x.AttemptNumber,
            CorrelationId = x.CorrelationId,
            ProviderReference = x.ProviderReference,
            Result = x.Result,
            AttemptedAtUtc = x.AttemptedAtUtc
        }));

        var compensationRows = await dbContext.Compensations.Where(x => x.SettlementId == settlement.SettlementId).ToListAsync(ct);
        dbContext.Compensations.RemoveRange(compensationRows);
        dbContext.Compensations.AddRange(settlement.Compensations.Select(x => new MerchantSettlementCompensationEntity
        {
            CompensationId = x.CompensationId,
            SettlementId = settlement.SettlementId,
            Reason = x.Reason,
            ProviderReference = x.ProviderReference,
            RequestedAtUtc = x.RequestedAtUtc,
            CompletedAtUtc = x.CompletedAtUtc
        }));
    }

    private static MerchantSettlementEntity ToEntity(MerchantSettlementOrchestration settlement)
    {
        var entity = new MerchantSettlementEntity { SettlementId = settlement.SettlementId };
        Map(settlement, entity);
        return entity;
    }

    private static void Map(MerchantSettlementOrchestration value, MerchantSettlementEntity entity)
    {
        entity.PaymentDecisionId = value.PaymentDecisionId;
        entity.PaymentIntentId = value.PaymentIntentId;
        entity.MerchantId = value.MerchantId;
        entity.Route = (int)value.Route;
        entity.AmountMinor = value.AmountMinor;
        entity.Currency = value.Currency;
        entity.IdempotencyKey = value.IdempotencyKey;
        entity.Status = (int)value.Status;
        entity.ReasonCode = (int)value.ReasonCode;
        entity.CorrelationId = value.CorrelationId;
        entity.ProviderReference = value.ProviderReference;
        entity.CreatedAtUtc = value.CreatedAtUtc;
        entity.UpdatedAtUtc = value.UpdatedAtUtc;
        entity.CompletedAtUtc = value.CompletedAtUtc;
    }
}
