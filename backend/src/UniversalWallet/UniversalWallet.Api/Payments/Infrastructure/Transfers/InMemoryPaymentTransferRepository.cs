using UniversalWallet.Api.Payments.Application.Transfers;
using UniversalWallet.Api.Payments.Domain.Transfers;

namespace UniversalWallet.Api.Payments.Infrastructure.Transfers;

public sealed class InMemoryPaymentTransferRepository : IPaymentTransferRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, PaymentTransfer> _transfers = new();
    private readonly Dictionary<Guid, Guid> _byIntent = new();

    public Task<PaymentTransfer?> GetByIntentAsync(Guid paymentIntentId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_byIntent.TryGetValue(paymentIntentId, out var transferId) && _transfers.TryGetValue(transferId, out var transfer) ? transfer : null);
        }
    }

    public Task<PaymentTransfer?> GetAsync(Guid transferId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_transfers.TryGetValue(transferId, out var transfer) ? transfer : null);
        }
    }

    public Task<IReadOnlyList<PaymentTransfer>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<PaymentTransfer>>(_transfers.Values.OrderByDescending(x => x.ExecutedAt ?? DateTimeOffset.MinValue).ToList());
        }
    }

    public Task AddAsync(PaymentTransfer transfer, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _transfers[transfer.TransferId] = transfer;
            _byIntent[transfer.PaymentIntentId] = transfer.TransferId;
            return Task.CompletedTask;
        }
    }

    public Task UpdateAsync(PaymentTransfer transfer, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _transfers[transfer.TransferId] = transfer;
            _byIntent[transfer.PaymentIntentId] = transfer.TransferId;
            return Task.CompletedTask;
        }
    }
}
