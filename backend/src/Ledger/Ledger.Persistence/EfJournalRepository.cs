using AfriWallet.Ledger.Application;
using AfriWallet.Ledger.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Ledger.Persistence;

public sealed class EfJournalRepository(LedgerDbContext dbContext) : IJournalRepository
{
    public Task<bool> ExistsByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default) =>
        dbContext.JournalEntries.AnyAsync(journal => journal.CorrelationId == correlationId, cancellationToken);

    public async Task AddAsync(JournalEntry journalEntry, CancellationToken cancellationToken = default)
    {
        dbContext.JournalEntries.Add(ToEntity(journalEntry));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<JournalEntry?> GetAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken = default)
    {
        var entity = await QueryJournal()
            .SingleOrDefaultAsync(journal => journal.Id == journalEntryId.Value, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<JournalEntry?> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        var entity = await QueryJournal()
            .SingleOrDefaultAsync(journal => journal.CorrelationId == correlationId, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    private IQueryable<LedgerJournalEntity> QueryJournal() =>
        dbContext.JournalEntries
            .AsNoTracking()
            .Include(journal => journal.Lines);

    private static LedgerJournalEntity ToEntity(JournalEntry journalEntry) => new()
    {
        Id = journalEntry.Id.Value,
        CurrencyCode = journalEntry.CurrencyCode,
        BusinessReference = journalEntry.BusinessReference,
        CorrelationId = journalEntry.CorrelationId,
        PostedAtUtc = journalEntry.PostedAtUtc,
        Lines = journalEntry.Lines.Select((line, index) => new LedgerLineEntity
        {
            JournalEntryId = journalEntry.Id.Value,
            Position = index,
            AccountId = line.AccountId.Value,
            Side = (int)line.Side,
            AmountMinor = line.AmountMinor,
            Memo = line.Memo
        }).ToList()
    };

    private static JournalEntry ToDomain(LedgerJournalEntity entity) =>
        JournalEntry.Create(
            new JournalEntryId(entity.Id),
            entity.CurrencyCode,
            entity.BusinessReference,
            entity.CorrelationId,
            entity.PostedAtUtc,
            entity.Lines
                .OrderBy(line => line.Position)
                .Select(line => new LedgerLine(
                    new AccountId(line.AccountId),
                    (LedgerSide)line.Side,
                    line.AmountMinor,
                    line.Memo)));
}
