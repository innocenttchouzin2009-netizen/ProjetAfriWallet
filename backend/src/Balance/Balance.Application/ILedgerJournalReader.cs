using AfriWallet.Balance.Domain;
using AfriWallet.Ledger.Domain;

namespace AfriWallet.Balance.Application;

public interface ILedgerJournalReader
{
    Task<IReadOnlyList<JournalEntry>> ReadAsync(
        BalanceKey key,
        CancellationToken cancellationToken = default);
}
