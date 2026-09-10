using AfriWallet.Balance.Application;
using AfriWallet.Balance.Domain;
using AfriWallet.Ledger.Domain;
using AfriWallet.Ledger.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Balance.Infrastructure;

public sealed class EfLedgerJournalReader(LedgerDbContext dbContext) : ILedgerJournalReader
{
    public async Task<IReadOnlyList<JournalEntry>> ReadAsync(
        BalanceKey key,
        CancellationToken cancellationToken = default)
    {
        var accountId = key.AccountId.Value;
        var currencyCode = key.CurrencyCode;

        var entities = await dbContext.JournalEntries
            .AsNoTracking()
            .Where(journal =>
                journal.CurrencyCode == currencyCode &&
                journal.Lines.Any(line => line.AccountId == accountId))
            .Include(journal => journal.Lines)
            .ToListAsync(cancellationToken);

        return entities
            .OrderBy(journal => journal.PostedAtUtc)
            .ThenBy(journal => journal.Id)
            .Select(ToDomain)
            .ToArray();
    }

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
