using UniversalWallet.Api.Payments.Application.Settlements;
using UniversalWallet.Api.Payments.Domain.Settlements;

namespace UniversalWallet.Api.Payments.Infrastructure.Settlements;

public sealed class InMemorySettlementRepository : ISettlementRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, Settlement> _settlements = new();
    private readonly Dictionary<Guid, Guid> _byTransfer = new();

    public Task<Settlement?> GetByTransferIdAsync(Guid transferId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_byTransfer.TryGetValue(transferId, out var settlementId) && _settlements.TryGetValue(settlementId, out var settlement)
                ? settlement
                : null);
        }
    }

    public Task<Settlement?> GetAsync(Guid settlementId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_settlements.TryGetValue(settlementId, out var settlement) ? settlement : null);
        }
    }

    public Task<IReadOnlyList<Settlement>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<Settlement>>(_settlements.Values.OrderByDescending(x => x.SettledAt ?? x.FailedAt ?? x.CreatedAt).ToList());
        }
    }

    public Task AddAsync(Settlement settlement, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _settlements[settlement.SettlementId] = settlement;
            _byTransfer[settlement.TransferId] = settlement.SettlementId;
            return Task.CompletedTask;
        }
    }

    public Task UpdateAsync(Settlement settlement, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _settlements[settlement.SettlementId] = settlement;
            _byTransfer[settlement.TransferId] = settlement.SettlementId;
            return Task.CompletedTask;
        }
    }
}
