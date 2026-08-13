using Accounting.Domain.Accounts;
using Accounting.Domain.Journals;
using Accounting.Domain.Periods;

namespace Accounting.Application.Interfaces;

public interface IAccountingRepository
{
    Task AddAccountAsync(GeneralLedgerAccount account, CancellationToken cancellationToken);
    Task<GeneralLedgerAccount?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<GeneralLedgerAccount>> GetAccountsAsync(CancellationToken cancellationToken);

    Task AddPeriodAsync(AccountingPeriod period, CancellationToken cancellationToken);
    Task<AccountingPeriod?> GetPeriodAsync(Guid periodId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AccountingPeriod>> GetPeriodsAsync(CancellationToken cancellationToken);

    Task AddJournalEntryAsync(JournalEntry journalEntry, CancellationToken cancellationToken);
    Task<JournalEntry?> GetJournalEntryAsync(Guid journalEntryId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<JournalEntry>> GetJournalEntriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<JournalEntry>> GetJournalEntriesByPeriodAsync(Guid periodId, CancellationToken cancellationToken);
}