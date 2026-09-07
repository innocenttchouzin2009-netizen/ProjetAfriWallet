using IdentityService.Api.Auth.Application;

namespace IdentityService.Api.Auth.Abstractions;

public interface IAuthUserStore
{
    Task<AuthUser?> FindByNormalizedIdentifierAsync(
        string normalizedIdentifier,
        CancellationToken cancellationToken = default);

    Task<bool> TryAddAsync(
        AuthUser user,
        CancellationToken cancellationToken = default);
}
