namespace AfriWallet.Wallet.Application;

public sealed class MobileWalletReadApplicationService(
    IWalletRepository walletRepository,
    IMobileWalletBalanceReader balanceReader)
{
    public async Task<WalletOperationResult<MobileWalletReadResult>> ListAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        if (ownerId == Guid.Empty)
        {
            return WalletOperationResult<MobileWalletReadResult>.Failure(
                WalletErrorCode.ValidationError,
                "Owner id is required.");
        }

        var wallets = await walletRepository.ListByOwnerAsync(ownerId, cancellationToken);
        var items = new List<MobileWalletReadItem>(wallets.Count);

        foreach (var wallet in wallets)
        {
            var availableMinor = await balanceReader.ReadAvailableMinorAsync(
                wallet.Id.Value,
                wallet.Currency.Code,
                cancellationToken);

            items.Add(new MobileWalletReadItem(
                wallet.Id.Value,
                wallet.Currency.Code,
                availableMinor,
                wallet.Status.ToString().ToUpperInvariant(),
                wallet.CountryCode?.Value));
        }

        return WalletOperationResult<MobileWalletReadResult>.Success(
            new MobileWalletReadResult(items));
    }
}
