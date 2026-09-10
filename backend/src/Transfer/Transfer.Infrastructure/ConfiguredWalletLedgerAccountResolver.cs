using AfriWallet.Ledger.Domain;
using AfriWallet.Transfer.Application;

namespace AfriWallet.Transfer.Infrastructure;

public sealed class ConfiguredWalletLedgerAccountResolver : IWalletLedgerAccountResolver
{
    private readonly IReadOnlyDictionary<Guid, AccountId> mappings;

    public ConfiguredWalletLedgerAccountResolver(IReadOnlyDictionary<Guid, AccountId> mappings)
    {
        ArgumentNullException.ThrowIfNull(mappings);
        this.mappings = mappings;
    }

    public Task<AccountId?> ResolveAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(mappings.TryGetValue(walletId, out var accountId) ? (AccountId?)accountId : null);
    }
}
