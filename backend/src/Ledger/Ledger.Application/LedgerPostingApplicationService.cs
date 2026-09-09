using AfriWallet.Ledger.Domain;

namespace AfriWallet.Ledger.Application;

public sealed class LedgerPostingApplicationService(IJournalRepository repository)
{
    public async Task<LedgerOperationResult<JournalEntryView>> PostAsync(
        PostJournalCommand command,
        DateTimeOffset postedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CorrelationId == Guid.Empty)
        {
            return LedgerOperationResult<JournalEntryView>.Failure(
                LedgerErrorCode.ValidationError,
                "Correlation id is required.");
        }

        if (await repository.ExistsByCorrelationIdAsync(command.CorrelationId, cancellationToken))
        {
            return LedgerOperationResult<JournalEntryView>.Failure(
                LedgerErrorCode.DuplicateCorrelation,
                "A journal entry already exists for this correlation id.");
        }

        try
        {
            var lines = command.Lines?.Select(line => new LedgerLine(
                new AccountId(line.AccountId),
                line.Side,
                line.AmountMinor,
                line.Memo)).ToArray()
                ?? throw new ArgumentException("Ledger lines are required.", nameof(command));

            var journal = JournalEntry.Create(
                JournalEntryId.New(),
                command.CurrencyCode,
                command.BusinessReference,
                command.CorrelationId,
                postedAtUtc,
                lines);

            await repository.AddAsync(journal, cancellationToken);
            return LedgerOperationResult<JournalEntryView>.Success(ToView(journal));
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException or OverflowException)
        {
            return LedgerOperationResult<JournalEntryView>.Failure(
                LedgerErrorCode.ValidationError,
                exception.Message);
        }
    }

    public async Task<LedgerOperationResult<JournalEntryView>> GetAsync(
        Guid journalEntryId,
        CancellationToken cancellationToken = default)
    {
        if (journalEntryId == Guid.Empty)
        {
            return LedgerOperationResult<JournalEntryView>.Failure(LedgerErrorCode.NotFound, "Journal entry not found.");
        }

        var journal = await repository.GetAsync(new JournalEntryId(journalEntryId), cancellationToken);
        return journal is null
            ? LedgerOperationResult<JournalEntryView>.Failure(LedgerErrorCode.NotFound, "Journal entry not found.")
            : LedgerOperationResult<JournalEntryView>.Success(ToView(journal));
    }

    public async Task<LedgerOperationResult<JournalEntryView>> GetByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
        {
            return LedgerOperationResult<JournalEntryView>.Failure(LedgerErrorCode.NotFound, "Journal entry not found.");
        }

        var journal = await repository.GetByCorrelationIdAsync(correlationId, cancellationToken);
        return journal is null
            ? LedgerOperationResult<JournalEntryView>.Failure(LedgerErrorCode.NotFound, "Journal entry not found.")
            : LedgerOperationResult<JournalEntryView>.Success(ToView(journal));
    }

    private static JournalEntryView ToView(JournalEntry journal) =>
        new(
            journal.Id.Value,
            journal.CurrencyCode,
            journal.BusinessReference,
            journal.CorrelationId,
            journal.PostedAtUtc,
            journal.Lines.Select(line => new LedgerLineView(
                line.AccountId.Value,
                line.Side,
                line.AmountMinor,
                line.Memo)).ToArray());
}
