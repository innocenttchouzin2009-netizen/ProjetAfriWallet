namespace AfriWallet.Wallet.Application;

public interface IMobileWalletBalanceReader
{
    Task<long> ReadAvailableMinorAsync(
        Guid walletId,
        string currencyCode,
        CancellationToken cancellationToken = default);
}
