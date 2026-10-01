using AfriWallet.Ledger.Domain;
using AfriWallet.TransactionRead.Application.Contracts;

namespace AfriWallet.TransactionRead.Application.Abstractions;

public interface ITransactionLedgerReader
{
    Task<IReadOnlyList<JournalEntry>> ReadAsync(
        AccountId walletAccountId,
        TransactionReadCursor? cursor,
        int take,
        CancellationToken cancellationToken = default);
}
