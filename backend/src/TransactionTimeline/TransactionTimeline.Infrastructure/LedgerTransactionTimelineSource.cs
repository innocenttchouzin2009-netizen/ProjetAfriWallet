using AfriWallet.Ledger.Domain;
using AfriWallet.Ledger.Persistence;
using AfriWallet.TransactionTimeline.Application.Contracts;
using AfriWallet.TransactionTimeline.Application.Sources;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.TransactionTimeline.Infrastructure;

public sealed class LedgerTransactionTimelineSource : ILedgerTransactionTimelineSource
{
    private readonly LedgerDbContext dbContext;
    private readonly IReadOnlyDictionary<Guid, AccountId> accountByWallet;

    public LedgerTransactionTimelineSource(
        LedgerDbContext dbContext,
        IReadOnlyDictionary<Guid, AccountId> walletLedgerAccountMappings)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        ArgumentNullException.ThrowIfNull(walletLedgerAccountMappings);

        var mappings = new Dictionary<Guid, AccountId>();
        var assignedAccounts = new HashSet<AccountId>();

        foreach (var pair in walletLedgerAccountMappings)
        {
            if (pair.Key == Guid.Empty)
            {
                throw new ArgumentException(
                    "Wallet id mapping cannot be empty.",
                    nameof(walletLedgerAccountMappings));
            }

            if (!assignedAccounts.Add(pair.Value))
            {
                throw new ArgumentException(
                    "A ledger account cannot be mapped to more than one wallet.",
                    nameof(walletLedgerAccountMappings));
            }

            mappings.Add(pair.Key, pair.Value);
        }

        accountByWallet = mappings;
    }

    public async Task<TransactionTimelinePage> ReadAsync(
        TransactionTimelineReadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (request.WalletId == Guid.Empty)
        {
            throw new ArgumentException("WalletId is required.", nameof(request));
        }

        if (request.Limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.Limit,
                "Timeline limit must be between 1 and 100.");
        }

        if (!accountByWallet.TryGetValue(request.WalletId, out var accountId))
        {
            return new TransactionTimelinePage([], false, null);
        }

        var beforeUtc = request.Before?.ToUniversalTime();
        var accountGuid = accountId.Value;

        var query = dbContext.JournalEntries
            .AsNoTracking()
            .Where(journal => journal.Lines.Any(line => line.AccountId == accountGuid))
            .Include(journal => journal.Lines)
            .AsQueryable();

        if (beforeUtc is not null)
        {
            query = query.Where(journal => journal.PostedAtUtc < beforeUtc.Value);
        }

        var journals = await query.ToListAsync(cancellationToken);

        var candidates = journals
            .Select(journal => ProjectJournal(journal, accountGuid))
            .Where(item => item is not null)
            .Select(item => item!)
            .OrderByDescending(item => item.OccurredAt)
            .ThenByDescending(item => item.TransactionId, StringComparer.Ordinal)
            .Take(request.Limit + 1)
            .ToArray();

        var hasMore = candidates.Length > request.Limit;
        var items = candidates.Take(request.Limit).ToArray();
        DateTimeOffset? nextBefore = hasMore && items.Length > 0
            ? items[^1].OccurredAt
            : null;

        return new TransactionTimelinePage(items, hasMore, nextBefore);
    }

    private static TransactionTimelineItem? ProjectJournal(
        LedgerJournalEntity journal,
        Guid accountId)
    {
        long debitMinor = 0;
        long creditMinor = 0;

        checked
        {
            foreach (var line in journal.Lines.Where(line => line.AccountId == accountId))
            {
                switch ((LedgerSide)line.Side)
                {
                    case LedgerSide.Debit:
                        debitMinor += line.AmountMinor;
                        break;
                    case LedgerSide.Credit:
                        creditMinor += line.AmountMinor;
                        break;
                    default:
                        throw new InvalidOperationException(
                            "Ledger journal contains an unsupported side.");
                }
            }
        }

        if (debitMinor == creditMinor)
        {
            return null;
        }

        var incoming = creditMinor > debitMinor;
        var amountMinor = incoming
            ? checked(creditMinor - debitMinor)
            : checked(debitMinor - creditMinor);

        var reference = journal.BusinessReference.Trim();
        if (reference.Length == 0)
        {
            throw new InvalidOperationException(
                "Ledger journal business reference is required for timeline projection.");
        }

        var currencyCode = journal.CurrencyCode.Trim().ToUpperInvariant();
        if (currencyCode.Length != 3)
        {
            throw new InvalidOperationException(
                "Ledger journal currency code must contain exactly three characters.");
        }

        return new TransactionTimelineItem(
            journal.Id.ToString("D"),
            amountMinor,
            currencyCode,
            incoming
                ? TransactionTimelineDirection.Incoming
                : TransactionTimelineDirection.Outgoing,
            TransactionTimelineStatus.Completed,
            journal.PostedAtUtc.ToUniversalTime(),
            reference,
            null);
    }
}
