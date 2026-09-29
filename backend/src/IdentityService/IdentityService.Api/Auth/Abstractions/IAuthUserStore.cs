using IdentityService.Api.Auth.Application;

namespace IdentityService.Api.Auth.Abstractions;

public interface IAuthUserStore
{
    Task<AuthUser?> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<AuthUser?> FindByNormalizedIdentifierAsync(
        string normalizedIdentifier,
        CancellationToken cancellationToken = default);

    Task<bool> TryAddAsync(
        AuthUser user,
        CancellationToken cancellationToken = default);
}
