using AfriWallet.Balance.Domain;

namespace AfriWallet.Balance.Application;

public sealed class LedgerBackedBalanceReadService(
    ILedgerJournalReader journalReader,
    BalanceProjectionService projectionService)
{
    public async Task<AccountBalanceSnapshot> ReadAsync(
        BalanceKey key,
        CancellationToken cancellationToken = default)
    {
        var journalEntries = await journalReader.ReadAsync(key, cancellationToken);
        if (journalEntries is null)
        {
            throw new InvalidOperationException("Ledger journal reader returned no collection.");
        }

        return projectionService.Project(key, journalEntries);
    }
}
