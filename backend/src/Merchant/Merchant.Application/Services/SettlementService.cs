using AfriWallet.Merchant.Domain.Entities;

namespace AfriWallet.Merchant.Application.Services;

public sealed class SettlementService
{
    private readonly List<MerchantSettlement> _settlements = [];

    public Task<IReadOnlyList<MerchantSettlement>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<MerchantSettlement>>(_settlements);

    public Task<MerchantSettlement> CreateAsync(MerchantSettlement settlement, CancellationToken cancellationToken = default)
    {
        settlement.SettlementId = string.IsNullOrWhiteSpace(settlement.SettlementId) ? Guid.NewGuid().ToString("N") : settlement.SettlementId;
        settlement.CreatedAt = settlement.CreatedAt == default ? DateTimeOffset.UtcNow : settlement.CreatedAt;
        _settlements.Add(settlement);
        return Task.FromResult(settlement);
    }
}
