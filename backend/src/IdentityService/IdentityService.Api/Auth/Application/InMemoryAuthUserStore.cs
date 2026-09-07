using System.Collections.Concurrent;
using IdentityService.Api.Auth.Abstractions;

namespace IdentityService.Api.Auth.Application;

public sealed class InMemoryAuthUserStore : IAuthUserStore
{
    private readonly ConcurrentDictionary<string, AuthUser> _users =
        new(StringComparer.Ordinal);

    public Task<AuthUser?> FindByNormalizedIdentifierAsync(
        string normalizedIdentifier,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _users.TryGetValue(normalizedIdentifier, out var user);
        return Task.FromResult(user);
    }

    public Task<bool> TryAddAsync(
        AuthUser user,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_users.TryAdd(user.NormalizedIdentifier, user));
    }
}
