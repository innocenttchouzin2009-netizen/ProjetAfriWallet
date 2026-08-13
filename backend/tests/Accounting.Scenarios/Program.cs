using Accounting.Application.Services;
using Accounting.Domain.Accounts;
using Accounting.Domain.Entries;
using Accounting.Infrastructure.Repositories;

var repository = new InMemoryAccountingRepository();
var ledgerService = new GeneralLedgerService(repository);
var reversalService = new JournalReversalService(repository, ledgerService);

var cashAccount = await ledgerService.CreateAccountAsync(
    "GL-CASH-XAF",
    "General Ledger Cash XAF",
    "XAF",
    GeneralLedgerAccountType.Asset,
    CancellationToken.None);

var revenueAccount = await ledgerService.CreateAccountAsync(
    "GL-FEES-XAF",
    "General Ledger Fees XAF",
    "XAF",
    GeneralLedgerAccountType.Revenue,
    CancellationToken.None);

var period = await ledgerService.OpenPeriodAsync(
    "2026-01",
    new DateOnly(2026, 1, 1),
    new DateOnly(2026, 1, 31),
    CancellationToken.None);

Assert(period.Status == Accounting.Domain.Periods.AccountingPeriodStatus.Open, "period opening");

var journalEntry = await ledgerService.PostJournalEntryAsync(
    period.PeriodId,
    "GL-ENTRY-001",
    "Fee recognition",
    new[]
    {
        new JournalPostingLine(cashAccount.AccountId, "XAF", 5_000_000, JournalLineSide.Debit, "cash receipt"),
        new JournalPostingLine(revenueAccount.AccountId, "XAF", 5_000_000, JournalLineSide.Credit, "earned revenue")
    },
    null,
    CancellationToken.None);

Assert(journalEntry.Status == Accounting.Domain.Journals.JournalEntryStatus.Posted, "journal posting");
Assert(journalEntry.Entries.Sum(x => x.DebitMinor) == journalEntry.Entries.Sum(x => x.CreditMinor), "journal balance");

var reversal = await reversalService.ReverseAsync(
    journalEntry.JournalEntryId,
    "GL-REV-001",
    "correcting reversal",
    CancellationToken.None);

Assert(reversal.SourceJournalEntryId == journalEntry.JournalEntryId, "reversal linkage");

var trialBalance = await ledgerService.GetTrialBalanceAsync(period.PeriodId, CancellationToken.None);
Assert(trialBalance.Sum(x => x.NetMinor) == 0, "trial balance net zero");

Console.WriteLine("period protection ....... PASS");
Console.WriteLine("reversal generation ..... PASS");
Console.WriteLine("trial balance ........... PASS");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0013.5 accounting and general ledger scenarios passed.");

static void Assert(bool condition, string scenario)
{
    if (!condition)
    {
        Console.WriteLine($"{scenario} ........ FAIL");
        Environment.ExitCode = 1;
        throw new InvalidOperationException($"Scenario failed: {scenario}");
    }

    Console.WriteLine($"{scenario} ........ PASS");
}
