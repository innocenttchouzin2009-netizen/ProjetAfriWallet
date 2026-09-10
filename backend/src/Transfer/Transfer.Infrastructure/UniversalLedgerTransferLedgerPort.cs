using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;
using AfriWallet.Transfer.Application;

namespace AfriWallet.Transfer.Infrastructure;

public sealed class UniversalLedgerTransferLedgerPort(IJournalRepository journalRepository) : ITransferLedgerPort
{
    public Task<bool> ExistsByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default) =>
        journalRepository.ExistsByCorrelationIdAsync(correlationId, cancellationToken);

    public Task PostAsync(
        JournalEntry journalEntry,
        CancellationToken cancellationToken = default) =>
        journalRepository.AddAsync(journalEntry, cancellationToken);
}
