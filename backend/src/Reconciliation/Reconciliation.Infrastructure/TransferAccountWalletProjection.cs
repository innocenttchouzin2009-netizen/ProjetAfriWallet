using AfriWallet.Ledger.Domain;
using AfriWallet.Reconciliation.Application;
using AfriWallet.Transfer.Application;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.Reconciliation.Infrastructure;

public sealed class TransferAccountWalletProjection : ITransferAccountWalletProjection
{
    private readonly IReadOnlyDictionary<AccountId, Guid> walletIdsByAccount;
    private readonly ITransferWalletReader transferWalletReader;
    private readonly IWalletRepository walletRepository;

    public TransferAccountWalletProjection(
        IReadOnlyDictionary<Guid, AccountId> walletAccountMappings,
        ITransferWalletReader transferWalletReader,
        IWalletRepository walletRepository)
    {
        ArgumentNullException.ThrowIfNull(walletAccountMappings);
        this.transferWalletReader = transferWalletReader ?? throw new ArgumentNullException(nameof(transferWalletReader));
        this.walletRepository = walletRepository ?? throw new ArgumentNullException(nameof(walletRepository));

        var inverse = new Dictionary<AccountId, Guid>();
        foreach (var mapping in walletAccountMappings)
        {
            if (mapping.Key == Guid.Empty)
            {
                throw new ArgumentException("Wallet/account mapping contains an empty wallet id.", nameof(walletAccountMappings));
            }

            if (mapping.Value.Value == Guid.Empty)
            {
                throw new ArgumentException("Wallet/account mapping contains an empty account id.", nameof(walletAccountMappings));
            }

            if (!inverse.TryAdd(mapping.Value, mapping.Key))
            {
                throw new ArgumentException("A ledger account cannot map to multiple wallets.", nameof(walletAccountMappings));
            }
        }

        walletIdsByAccount = inverse;
    }

    public async Task<ReconciliationWalletProjection?> ResolveAsync(
        AccountId accountId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!walletIdsByAccount.TryGetValue(accountId, out var walletId))
        {
            return null;
        }

        var transferWallet = await transferWalletReader.GetAsync(walletId, cancellationToken);
        var wallet = await walletRepository.GetAsync(WalletId.From(walletId), cancellationToken);
        if (transferWallet is null || wallet is null)
        {
            return null;
        }

        if (transferWallet.WalletId != walletId || transferWallet.AccountId != accountId)
        {
            throw new InvalidOperationException("Transfer wallet adapter returned data inconsistent with the configured wallet/account mapping.");
        }

        if (!string.Equals(transferWallet.CurrencyCode, wallet.Currency.Code, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Transfer wallet adapter currency does not match Wallet Registry currency.");
        }

        return new ReconciliationWalletProjection(
            wallet.Id.Value,
            wallet.OwnerId,
            accountId,
            wallet.Currency.Code,
            transferWallet.IsActive);
    }
}
