using AfriWallet.Ledger.Domain;
using AfriWallet.TransactionHistory.Application.Abstractions;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.TransactionHistory.Infrastructure;

public sealed class ConfiguredTransactionHistoryLedgerAccountResolver
    : ITransactionHistoryLedgerAccountResolver
{
    private readonly IReadOnlyDictionary<Guid, AccountId> mappings;

    public ConfiguredTransactionHistoryLedgerAccountResolver(
        IReadOnlyDictionary<Guid, AccountId> mappings)
    {
        ArgumentNullException.ThrowIfNull(mappings);
        this.mappings = mappings;
    }

    public Task<AccountId?> ResolveAsync(
        WalletId walletId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(
            mappings.TryGetValue(walletId.Value, out var accountId)
                ? (AccountId?)accountId
                : null);
    }
}
