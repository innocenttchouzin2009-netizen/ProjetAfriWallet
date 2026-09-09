using AfriWallet.Ledger.Domain;

namespace AfriWallet.Ledger.Application;

public interface IJournalRepository
{
    Task<bool> ExistsByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default);
    Task AddAsync(JournalEntry journalEntry, CancellationToken cancellationToken = default);
    Task<JournalEntry?> GetAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken = default);
    Task<JournalEntry?> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default);
}
