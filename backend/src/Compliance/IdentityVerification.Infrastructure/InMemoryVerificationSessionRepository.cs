using System.Collections.Concurrent;
using AfriWallet.Compliance.IdentityVerification.Application.Abstractions;
using AfriWallet.Compliance.IdentityVerification.Domain.Sessions;

namespace AfriWallet.Compliance.IdentityVerification.Infrastructure;

public sealed class InMemoryVerificationSessionRepository : IVerificationSessionRepository
{
    private readonly ConcurrentDictionary<Guid, VerificationSession> _sessions = new();

    public Task<VerificationSession?> GetAsync(VerificationSessionId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _sessions.TryGetValue(id.Value, out var session);
        return Task.FromResult(session);
    }

    public Task<VerificationSession?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = _sessions.Values.FirstOrDefault(x => string.Equals(x.IdempotencyKey, idempotencyKey, StringComparison.Ordinal));
        return Task.FromResult(session);
    }

    public Task AddAsync(VerificationSession session, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_sessions.TryAdd(session.Id.Value, session))
        {
            throw new InvalidOperationException("Verification session already exists.");
        }

        return Task.CompletedTask;
    }

    public Task SaveAsync(VerificationSession session, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _sessions[session.Id.Value] = session;
        return Task.CompletedTask;
    }
}
