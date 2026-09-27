using AfriWallet.Balance.Application;
using AfriWallet.Balance.Domain;
using AfriWallet.Timeline.Application;
using AfriWallet.Timeline.Domain;
using AfriWallet.Wallet.Application;

namespace AfriWallet.Timeline.Infrastructure;

public sealed class LedgerBackedFinancialActivityReader(
    IWalletRepository walletRepository,
    IFinancialActivityWalletAccountResolver accountResolver,
    ILedgerJournalReader journalReader,
    FinancialActivityProjectionService projectionService) : IFinancialActivityReader
{
    public async Task<FinancialActivityResult> ReadAsync(
        FinancialActivityQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var wallets = await walletRepository.ListByOwnerAsync(query.OwnerId, cancellationToken);
        var selectedWallets = query.WalletId is null
            ? wallets
            : wallets.Where(wallet => wallet.Id == query.WalletId.Value).ToArray();

        var activities = new List<FinancialActivity>();

        foreach (var wallet in selectedWallets.OrderBy(wallet => wallet.Id.Value))
        {
            var accountId = await accountResolver.ResolveAsync(wallet.Id, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Wallet '{wallet.Id.Value}' has no resolved ledger account.");

            var journalEntries = await journalReader.ReadAsync(
                new BalanceKey(accountId, wallet.Currency.Code),
                cancellationToken);

            foreach (var journalEntry in journalEntries)
            {
                var activity = projectionService.Project(
                    query.OwnerId,
                    wallet.Id,
                    accountId,
                    journalEntry);

                if (activity is not null)
                    activities.Add(activity);
            }
        }

        var ordered = activities
            .OrderByDescending(activity => activity.OccurredAtUtc)
            .ThenBy(activity => activity.WalletId.Value)
            .ThenBy(activity => activity.Source.Key, StringComparer.Ordinal)
            .ToArray();

        return FinancialActivityResult.Create(ordered);
    }
}
