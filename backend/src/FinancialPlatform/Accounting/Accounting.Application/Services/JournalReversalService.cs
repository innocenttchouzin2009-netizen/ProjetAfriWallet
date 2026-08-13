using Accounting.Application.Interfaces;
using Accounting.Domain.Entries;
using Accounting.Domain.Journals;

namespace Accounting.Application.Services;

public sealed class JournalReversalService
{
    private readonly IAccountingRepository _repository;
    private readonly GeneralLedgerService _generalLedgerService;

    public JournalReversalService(IAccountingRepository repository, GeneralLedgerService generalLedgerService)
    {
        _repository = repository;
        _generalLedgerService = generalLedgerService;
    }

    public async Task<JournalEntry> ReverseAsync(
        Guid originalJournalEntryId,
        string reference,
        string reason,
        CancellationToken cancellationToken)
    {
        var original = await _repository.GetJournalEntryAsync(originalJournalEntryId, cancellationToken)
            ?? throw new KeyNotFoundException("Original journal entry not found.");

        if (original.Status != JournalEntryStatus.Posted)
            throw new InvalidOperationException("Only posted journal entries can be reversed.");

        var reverseLines = original.Entries
            .Select(line => new JournalPostingLine(
                line.AccountId,
                line.CurrencyCode,
                line.DebitMinor > 0 ? line.DebitMinor : line.CreditMinor,
                line.DebitMinor > 0 ? JournalLineSide.Credit : JournalLineSide.Debit,
                line.Narration))
            .ToArray();

        return await _generalLedgerService.PostJournalEntryAsync(
            original.PeriodId,
            reference,
            reason,
            reverseLines,
            originalJournalEntryId,
            cancellationToken);
    }
}