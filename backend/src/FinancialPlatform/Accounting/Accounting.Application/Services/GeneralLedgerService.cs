using Accounting.Application.Interfaces;
using Accounting.Application.Validation;
using Accounting.Domain.Accounts;
using Accounting.Domain.Entries;
using Accounting.Domain.Journals;
using Accounting.Domain.Periods;
using Accounting.Domain.TrialBalance;

namespace Accounting.Application.Services;

public sealed class GeneralLedgerService
{
    private readonly IAccountingRepository _repository;

    public GeneralLedgerService(IAccountingRepository repository)
    {
        _repository = repository;
    }

    public async Task<GeneralLedgerAccount> CreateAccountAsync(
        string accountCode,
        string displayName,
        string currencyCode,
        GeneralLedgerAccountType type,
        CancellationToken cancellationToken)
    {
        var account = new GeneralLedgerAccount(
            Guid.NewGuid(),
            AccountingValidation.RequireText(accountCode, nameof(accountCode)),
            AccountingValidation.RequireText(displayName, nameof(displayName)),
            AccountingValidation.NormalizeCurrencyCode(currencyCode),
            type);

        await _repository.AddAccountAsync(account, cancellationToken);
        return account;
    }

    public async Task<AccountingPeriod> OpenPeriodAsync(
        string periodCode,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var period = new AccountingPeriod(
            Guid.NewGuid(),
            AccountingValidation.RequireText(periodCode, nameof(periodCode)),
            startDate,
            endDate);

        period.Open();
        await _repository.AddPeriodAsync(period, cancellationToken);
        return period;
    }

    public async Task<JournalEntry> PostJournalEntryAsync(
        Guid periodId,
        string reference,
        string description,
        IReadOnlyCollection<JournalPostingLine> lines,
        Guid? sourceJournalEntryId,
        CancellationToken cancellationToken)
    {
        var period = await RequireOpenPeriodAsync(periodId, cancellationToken);

        if (lines.Count == 0)
            throw new InvalidOperationException("Journal entry requires at least one line.");

        var normalizedCurrency = AccountingValidation.NormalizeCurrencyCode(lines.First().CurrencyCode);
        var journalEntry = new JournalEntry(
            Guid.NewGuid(),
            period.PeriodId,
            AccountingValidation.RequireText(reference, nameof(reference)),
            AccountingValidation.RequireText(description, nameof(description)),
            sourceJournalEntryId);

        foreach (var line in lines)
        {
            var account = await RequireActiveAccountAsync(line.AccountId, cancellationToken);
            var lineCurrency = AccountingValidation.NormalizeCurrencyCode(line.CurrencyCode);

            if (!string.Equals(lineCurrency, normalizedCurrency, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Journal entry lines must use a single currency.");

            if (!string.Equals(account.CurrencyCode, lineCurrency, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Account currency mismatch.");

            AccountingValidation.RequirePositiveAmount(line.AmountMinor, nameof(line.AmountMinor));

            if (line.Side == JournalLineSide.Debit)
            {
                journalEntry.AddDebit(account.AccountId, lineCurrency, line.AmountMinor, line.Narration);
                continue;
            }

            journalEntry.AddCredit(account.AccountId, lineCurrency, line.AmountMinor, line.Narration);
        }

        journalEntry.Post(DateTime.UtcNow);
        await _repository.AddJournalEntryAsync(journalEntry, cancellationToken);
        return journalEntry;
    }

    public async Task<IReadOnlyCollection<TrialBalanceLine>> GetTrialBalanceAsync(
        Guid periodId,
        CancellationToken cancellationToken)
    {
        await RequireOpenPeriodAsync(periodId, cancellationToken);

        var journalEntries = await _repository.GetJournalEntriesByPeriodAsync(periodId, cancellationToken);
        var accounts = await _repository.GetAccountsAsync(cancellationToken);

        var lines = journalEntries
            .SelectMany(entry => entry.Entries)
            .GroupBy(line => line.AccountId)
            .Select(group =>
            {
                var account = accounts.First(x => x.AccountId == group.Key);
                var debitMinor = group.Sum(x => x.DebitMinor);
                var creditMinor = group.Sum(x => x.CreditMinor);
                return new TrialBalanceLine(
                    account.AccountId,
                    account.AccountCode,
                    account.DisplayName,
                    account.CurrencyCode,
                    debitMinor,
                    creditMinor,
                    debitMinor - creditMinor);
            })
            .OrderBy(x => x.AccountCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return lines;
    }

    private async Task<GeneralLedgerAccount> RequireActiveAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var account = await _repository.GetAccountAsync(accountId, cancellationToken)
            ?? throw new KeyNotFoundException("General ledger account not found.");

        if (account.Status != GeneralLedgerAccountStatus.Active)
            throw new InvalidOperationException("General ledger account is not active.");

        return account;
    }

    private async Task<AccountingPeriod> RequireOpenPeriodAsync(Guid periodId, CancellationToken cancellationToken)
    {
        var period = await _repository.GetPeriodAsync(periodId, cancellationToken)
            ?? throw new KeyNotFoundException("Accounting period not found.");

        if (period.Status != AccountingPeriodStatus.Open)
            throw new InvalidOperationException("Accounting period is not open.");

        return period;
    }
}