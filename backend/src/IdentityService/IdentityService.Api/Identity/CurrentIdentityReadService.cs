using AfriWallet.P2P.Directory.Persistence;

namespace IdentityService.Api.Identity;

public sealed class CurrentIdentityReadService(
    CurrentProfileReadService profiles,
    ICurrentAfWalIdentityReader afWalIdentities)
{
    public async Task<CurrentIdentityReadModel?> ReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        var profile = await profiles.ReadAsync(userId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        var afWalIdentity = await afWalIdentities.FindByOwnerIdAsync(userId, cancellationToken);
        return afWalIdentity is null
            ? null
            : new CurrentIdentityReadModel(
                profile.UserId,
                profile.Identifier,
                profile.CreatedAtUtc,
                afWalIdentity.AfWalId);
    }
}
