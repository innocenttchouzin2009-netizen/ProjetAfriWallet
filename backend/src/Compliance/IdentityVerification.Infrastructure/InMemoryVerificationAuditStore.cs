using System.Collections.Concurrent;
using AfriWallet.Compliance.IdentityVerification.Application.Abstractions;

namespace AfriWallet.Compliance.IdentityVerification.Infrastructure;

public sealed class InMemoryVerificationAuditStore : IVerificationAuditStore
{
    private readonly ConcurrentQueue<VerificationAuditEvent> _events = new();

    public Task AppendAsync(VerificationAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _events.Enqueue(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<VerificationAuditEvent>> GetBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<VerificationAuditEvent> result = _events
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.OccurredAtUtc)
            .ToArray();

        return Task.FromResult(result);
    }
}
