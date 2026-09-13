using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;

namespace AfriWallet.Reconciliation.Application;

public sealed class TransferReceiptLookupService(IJournalRepository journalRepository)
{
    public async Task<TransferReceiptLookupResult> GetByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException("Correlation id cannot be empty.", nameof(correlationId));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var journal = await journalRepository.GetByCorrelationIdAsync(correlationId, cancellationToken);
        return journal is null ? TransferReceiptLookupResult.NotFound() : FromJournal(journal);
    }

    public async Task<TransferReceiptLookupResult> GetByJournalEntryIdAsync(
        Guid journalEntryId,
        CancellationToken cancellationToken = default)
    {
        if (journalEntryId == Guid.Empty)
        {
            throw new ArgumentException("Journal entry id cannot be empty.", nameof(journalEntryId));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var journal = await journalRepository.GetAsync(new JournalEntryId(journalEntryId), cancellationToken);
        return journal is null ? TransferReceiptLookupResult.NotFound() : FromJournal(journal);
    }

    private static TransferReceiptLookupResult FromJournal(JournalEntry journal)
    {
        const string prefix = "TRF-";
        if (!journal.BusinessReference.StartsWith(prefix, StringComparison.Ordinal))
        {
            return TransferReceiptLookupResult.NotTransferJournal();
        }

        var transferToken = journal.BusinessReference[prefix.Length..];
        if (transferToken.Length != 32 || !Guid.TryParseExact(transferToken, "N", out var transferId) || transferId == Guid.Empty)
        {
            return TransferReceiptLookupResult.Invalid("Transfer business reference is malformed.");
        }

        if (journal.Lines.Count != 2)
        {
            return TransferReceiptLookupResult.Invalid("Transfer journal must contain exactly two ledger lines.");
        }

        var debit = journal.Lines.SingleOrDefault(line => line.Side == LedgerSide.Debit);
        var credit = journal.Lines.SingleOrDefault(line => line.Side == LedgerSide.Credit);
        if (debit is null || credit is null)
        {
            return TransferReceiptLookupResult.Invalid("Transfer journal must contain one debit and one credit line.");
        }

        if (debit.AccountId == credit.AccountId)
        {
            return TransferReceiptLookupResult.Invalid("Transfer debit and credit accounts must be different.");
        }

        if (debit.AmountMinor <= 0 || debit.AmountMinor != credit.AmountMinor)
        {
            return TransferReceiptLookupResult.Invalid("Transfer debit and credit amounts must match and be positive.");
        }

        return TransferReceiptLookupResult.Found(new TransferReceipt(
            transferId,
            journal.Id,
            journal.CorrelationId,
            journal.CurrencyCode,
            debit.AmountMinor,
            debit.AccountId,
            credit.AccountId,
            journal.PostedAtUtc,
            journal.BusinessReference));
    }
}
