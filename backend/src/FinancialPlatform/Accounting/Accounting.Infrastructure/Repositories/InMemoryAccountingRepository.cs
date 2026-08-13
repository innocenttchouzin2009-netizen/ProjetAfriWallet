using System.Collections.Concurrent;
using Accounting.Application.Interfaces;
using Accounting.Domain.Accounts;
using Accounting.Domain.Journals;
using Accounting.Domain.Periods;

namespace Accounting.Infrastructure.Repositories;

public sealed class InMemoryAccountingRepository : IAccountingRepository
{
    private readonly ConcurrentDictionary<Guid, GeneralLedgerAccount> _accounts = new();
    private readonly ConcurrentDictionary<Guid, AccountingPeriod> _periods = new();
    private readonly ConcurrentDictionary<Guid, JournalEntry> _journalEntries = new();

    public Task AddAccountAsync(GeneralLedgerAccount account, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_accounts.TryAdd(account.AccountId, account))
            throw new InvalidOperationException("General ledger account already exists.");

        return Task.CompletedTask;
    }

    public Task<GeneralLedgerAccount?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _accounts.TryGetValue(accountId, out var account);
        return Task.FromResult(account);
    }

    public Task<IReadOnlyCollection<GeneralLedgerAccount>> GetAccountsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<GeneralLedgerAccount>>(_accounts.Values.ToArray());
    }

    public Task AddPeriodAsync(AccountingPeriod period, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_periods.TryAdd(period.PeriodId, period))
            throw new InvalidOperationException("Accounting period already exists.");

        return Task.CompletedTask;
    }

    public Task<AccountingPeriod?> GetPeriodAsync(Guid periodId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _periods.TryGetValue(periodId, out var period);
        return Task.FromResult(period);
    }

    public Task<IReadOnlyCollection<AccountingPeriod>> GetPeriodsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<AccountingPeriod>>(_periods.Values.ToArray());
    }

    public Task AddJournalEntryAsync(JournalEntry journalEntry, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (journalEntry.Status != JournalEntryStatus.Posted)
            throw new InvalidOperationException("Only posted journal entries can be stored.");

        if (!_journalEntries.TryAdd(journalEntry.JournalEntryId, journalEntry))
            throw new InvalidOperationException("Journal entry already exists.");

        return Task.CompletedTask;
    }

    public Task<JournalEntry?> GetJournalEntryAsync(Guid journalEntryId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _journalEntries.TryGetValue(journalEntryId, out var journalEntry);
        return Task.FromResult(journalEntry);
    }

    public Task<IReadOnlyCollection<JournalEntry>> GetJournalEntriesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<JournalEntry>>(_journalEntries.Values.ToArray());
    }

    public Task<IReadOnlyCollection<JournalEntry>> GetJournalEntriesByPeriodAsync(Guid periodId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var journalEntries = _journalEntries.Values.Where(x => x.PeriodId == periodId).ToArray();
        return Task.FromResult<IReadOnlyCollection<JournalEntry>>(journalEntries);
    }
}