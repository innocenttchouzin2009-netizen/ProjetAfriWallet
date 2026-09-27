namespace AfriWallet.P2P.Infrastructure;

public sealed record IssuedReceiveIdentity(string PublicLabel, string QrToken);

public interface IReceiveIdentityIssuer
{
    Task<IssuedReceiveIdentity?> IssueAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);
}
