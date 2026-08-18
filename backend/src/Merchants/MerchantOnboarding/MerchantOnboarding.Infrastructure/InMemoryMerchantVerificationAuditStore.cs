using System.Collections.Concurrent;
using AfriWallet.Merchants.Onboarding.Application.Abstractions;

namespace AfriWallet.Merchants.Onboarding.Infrastructure;

public sealed class InMemoryMerchantVerificationAuditStore : IMerchantVerificationAuditStore
{
    private readonly ConcurrentQueue<MerchantVerificationAuditEvent> _events = new();

    public Task AppendAsync(MerchantVerificationAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        _events.Enqueue(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<MerchantVerificationAuditEvent>> GetAsync(Guid verificationId, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<MerchantVerificationAuditEvent> result = _events
            .Where(x => x.VerificationId == verificationId)
            .ToArray();
        return Task.FromResult(result);
    }
}
