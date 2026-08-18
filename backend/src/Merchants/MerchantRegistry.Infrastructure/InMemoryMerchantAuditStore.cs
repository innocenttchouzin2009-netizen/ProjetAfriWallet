using System.Collections.Concurrent;
using AfriWallet.Merchants.Registry.Application.Abstractions;

namespace AfriWallet.Merchants.Registry.Infrastructure;

public sealed class InMemoryMerchantAuditStore : IMerchantAuditStore
{
    private readonly ConcurrentQueue<MerchantAuditEvent> _events = new();

    public Task AppendAsync(MerchantAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _events.Enqueue(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<MerchantAuditEvent>> GetAsync(string merchantId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<MerchantAuditEvent> result = _events
            .Where(x => string.Equals(x.MerchantId, merchantId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return Task.FromResult(result);
    }
}
