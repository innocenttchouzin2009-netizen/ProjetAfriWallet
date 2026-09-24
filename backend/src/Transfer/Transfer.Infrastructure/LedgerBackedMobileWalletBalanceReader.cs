using AfriWallet.Balance.Application;
using AfriWallet.Balance.Domain;
using AfriWallet.Transfer.Application;
using AfriWallet.Wallet.Application;

namespace AfriWallet.Transfer.Infrastructure;

public sealed class LedgerBackedMobileWalletBalanceReader(
    IWalletLedgerAccountResolver accountResolver,
    LedgerBackedBalanceReadService balanceReadService,
    ITransferFundsAvailabilityPolicy availabilityPolicy) : IMobileWalletBalanceReader
{
    public async Task<long> ReadAvailableMinorAsync(
        Guid walletId,
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        if (walletId == Guid.Empty)
        {
            throw new ArgumentException("Wallet id cannot be empty.", nameof(walletId));
        }

        var accountId = await accountResolver.ResolveAsync(walletId, cancellationToken)
            ?? throw new InvalidOperationException("Wallet has no resolved Ledger account.");

        var snapshot = await balanceReadService.ReadAsync(
            new BalanceKey(accountId, currencyCode),
            cancellationToken);

        return availabilityPolicy.GetAvailableMinor(
            new TransferBalanceProjection(
                snapshot.DebitMinor,
                snapshot.CreditMinor,
                snapshot.NetMinor));
    }
}
