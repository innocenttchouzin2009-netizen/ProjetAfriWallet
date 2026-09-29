using IdentityService.Api.Auth.Abstractions;

namespace IdentityService.Api.Identity;

public sealed class CurrentProfileReadService(IAuthUserStore users)
{
    public async Task<CurrentProfileReadModel?> ReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        var user = await users.FindByIdAsync(userId, cancellationToken);
        return user is null
            ? null
            : new CurrentProfileReadModel(
                user.Id,
                user.NormalizedIdentifier,
                user.CreatedAtUtc);
    }
}
