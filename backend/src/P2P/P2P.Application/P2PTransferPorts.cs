namespace AfriWallet.P2P.Application;

public interface IP2PTransferPort
{
    Task<P2PTransferReceipt> ExecuteAsync(
        Guid sourceWalletId,
        Guid targetWalletId,
        long amountMinor,
        Guid correlationId,
        DateTimeOffset requestedAtUtc,
        CancellationToken cancellationToken = default);
}
