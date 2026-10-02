using AfriWallet.Ledger.Domain;
using AfriWallet.Ledger.Persistence;
using AfriWallet.TransactionHistory.Application.Abstractions;
using AfriWallet.TransactionHistory.Application.Contracts;
using AfriWallet.TransactionHistory.Application.Cursor;
using AfriWallet.TransactionHistory.Application.Projection;
using AfriWallet.Wallet.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.TransactionHistory.Infrastructure;

public sealed class LedgerBackedTransactionHistoryReader(
    LedgerDbContext dbContext,
    ITransactionHistoryLedgerAccountResolver accountResolver,
    TransactionHistoryProjector projector) : ITransactionHistoryReader
{
    public async Task<TransactionHistoryPage> ReadAsync(
        IReadOnlyCollection<WalletId> walletIds,
        TransactionHistoryPageRequest page,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(walletIds);
        ArgumentNullException.ThrowIfNull(page);
        cancellationToken.ThrowIfCancellationRequested();

        var validatedPage = TransactionHistoryPageRequest.Create(page.Limit, page.Cursor);

        var authorizedWalletIds = walletIds
            .Where(walletId => walletId.Value != Guid.Empty)
            .Distinct()
            .OrderBy(walletId => walletId.Value)
            .ToArray();

        if (authorizedWalletIds.Length == 0)
        {
            return new TransactionHistoryPage([], null);
        }

        var walletByAccountId = new Dictionary<Guid, WalletId>();

        foreach (var walletId in authorizedWalletIds)
        {
            var accountId = await accountResolver.ResolveAsync(walletId, cancellationToken);
            if (accountId is null)
            {
                continue;
            }

            if (!walletByAccountId.TryAdd(accountId.Value.Value, walletId))
            {
                throw new InvalidOperationException(
                    "Multiple authorized wallets resolve to the same ledger account.");
            }
        }

        if (walletByAccountId.Count == 0)
        {
            return new TransactionHistoryPage([], null);
        }

        var authorizedAccountIds = walletByAccountId.Keys.ToArray();

        var journals = await dbContext.JournalEntries
            .AsNoTracking()
            .Where(journal => journal.Lines.Any(line => authorizedAccountIds.Contains(line.AccountId)))
            .Include(journal => journal.Lines)
            .ToListAsync(cancellationToken);

        var items = journals
            .Select(journal => ProjectAuthorizedJournal(journal, walletByAccountId))
            .Where(item => item is not null)
            .Select(item => item!)
            .OrderByDescending(item => item.OccurredAtUtc)
            .ThenByDescending(item => item.TransactionId)
            .Where(item => IsAfterCursor(item, validatedPage.Cursor))
            .Take(validatedPage.Limit + 1)
            .ToArray();

        var hasMore = items.Length > validatedPage.Limit;
        var pageItems = hasMore
            ? items.Take(validatedPage.Limit).ToArray()
            : items;

        TransactionHistoryCursor? nextCursor = hasMore && pageItems.Length > 0
            ? new TransactionHistoryCursor(
                pageItems[^1].OccurredAtUtc,
                pageItems[^1].TransactionId)
            : null;

        return new TransactionHistoryPage(pageItems, nextCursor);
    }

    private TransactionHistoryItem? ProjectAuthorizedJournal(
        LedgerJournalEntity journal,
        IReadOnlyDictionary<Guid, WalletId> walletByAccountId)
    {
        var walletLine = journal.Lines
            .OrderBy(line => line.Position)
            .FirstOrDefault(line => walletByAccountId.ContainsKey(line.AccountId));

        if (walletLine is null)
        {
            return null;
        }

        if (journal.CorrelationId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Ledger journal '{journal.Id}' has an empty correlation id.");
        }

        if (walletLine.AmountMinor <= 0)
        {
            throw new InvalidOperationException(
                $"Ledger journal '{journal.Id}' contains a non-positive wallet amount.");
        }

        var source = new TransactionHistoryProjectionSource(
            journal.CorrelationId,
            walletByAccountId[walletLine.AccountId],
            walletLine.AmountMinor,
            journal.CurrencyCode,
            (LedgerSide)walletLine.Side,
            TransactionHistoryStatus.Completed,
            journal.PostedAtUtc,
            journal.BusinessReference,
            CounterpartyLabel: null);

        return projector.Project(source);
    }

    private static bool IsAfterCursor(
        TransactionHistoryItem item,
        TransactionHistoryCursor? cursor)
    {
        if (cursor is null)
        {
            return true;
        }

        if (item.OccurredAtUtc < cursor.Value.OccurredAtUtc)
        {
            return true;
        }

        return item.OccurredAtUtc == cursor.Value.OccurredAtUtc
            && item.TransactionId.CompareTo(cursor.Value.TransactionId) < 0;
    }
}
