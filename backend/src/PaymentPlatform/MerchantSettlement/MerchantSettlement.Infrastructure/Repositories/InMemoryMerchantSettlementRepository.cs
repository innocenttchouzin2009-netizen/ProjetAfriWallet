using System.Collections.Concurrent;
using MerchantSettlement.Application.Interfaces;
using MerchantSettlement.Domain.Batches;
using MerchantSettlement.Domain.Profiles;

namespace MerchantSettlement.Infrastructure.Repositories;

public sealed class InMemoryMerchantSettlementRepository : IMerchantSettlementRepository
{
    private readonly ConcurrentDictionary<string, MerchantSettlementProfile> _profiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Guid, MerchantSettlement.Domain.Settlements.MerchantSettlement> _settlements = new();
    private readonly ConcurrentDictionary<Guid, MerchantSettlementBatch> _batches = new();

    public Task AddProfileAsync(
        MerchantSettlementProfile profile,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_profiles.TryAdd(profile.MerchantId, profile))
            throw new InvalidOperationException("Merchant settlement profile already exists.");

        return Task.CompletedTask;
    }

    public Task<MerchantSettlementProfile?> GetProfileAsync(
        string merchantId,
        CancellationToken cancellationToken)
    {
        _profiles.TryGetValue(merchantId, out var profile);
        return Task.FromResult(profile);
    }

    public Task AddSettlementAsync(
        MerchantSettlement.Domain.Settlements.MerchantSettlement settlement,
        CancellationToken cancellationToken)
    {
        if (_settlements.Values.Any(x => string.Equals(x.IdempotencyKey, settlement.IdempotencyKey, StringComparison.Ordinal)))
            throw new InvalidOperationException("Settlement idempotency key already exists.");

        if (!_settlements.TryAdd(settlement.SettlementId, settlement))
            throw new InvalidOperationException("Merchant settlement already exists.");

        return Task.CompletedTask;
    }

    public Task<MerchantSettlement.Domain.Settlements.MerchantSettlement?> GetSettlementAsync(
        Guid settlementId,
        CancellationToken cancellationToken)
    {
        _settlements.TryGetValue(settlementId, out var settlement);
        return Task.FromResult(settlement);
    }

    public Task<MerchantSettlement.Domain.Settlements.MerchantSettlement?> GetSettlementByIdempotencyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            _settlements.Values.FirstOrDefault(x =>
                string.Equals(x.IdempotencyKey, idempotencyKey, StringComparison.Ordinal)));
    }

    public Task AddBatchAsync(
        MerchantSettlementBatch batch,
        CancellationToken cancellationToken)
    {
        if (!_batches.TryAdd(batch.BatchId, batch))
            throw new InvalidOperationException("Settlement batch already exists.");

        return Task.CompletedTask;
    }
}
