using MerchantSettlement.Domain.Batches;
using MerchantSettlement.Domain.Profiles;

namespace MerchantSettlement.Application.Interfaces;

public interface IMerchantSettlementRepository
{
    Task AddProfileAsync(
        MerchantSettlementProfile profile,
        CancellationToken cancellationToken);

    Task<MerchantSettlementProfile?> GetProfileAsync(
        string merchantId,
        CancellationToken cancellationToken);

    Task AddSettlementAsync(
        MerchantSettlement.Domain.Settlements.MerchantSettlement settlement,
        CancellationToken cancellationToken);

    Task<MerchantSettlement.Domain.Settlements.MerchantSettlement?> GetSettlementAsync(
        Guid settlementId,
        CancellationToken cancellationToken);

    Task<MerchantSettlement.Domain.Settlements.MerchantSettlement?> GetSettlementByIdempotencyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task AddBatchAsync(
        MerchantSettlementBatch batch,
        CancellationToken cancellationToken);
}
