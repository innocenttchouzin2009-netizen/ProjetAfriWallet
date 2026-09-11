namespace AfriWallet.P2P.Infrastructure;

public interface IAfWalIdentityDirectory
{
    Task<Guid?> ResolveOwnerIdAsync(string afWalId, CancellationToken cancellationToken = default);
}

public interface IQrRecipientDirectory
{
    Task<Guid?> ResolveOwnerIdAsync(string qrToken, CancellationToken cancellationToken = default);
}
