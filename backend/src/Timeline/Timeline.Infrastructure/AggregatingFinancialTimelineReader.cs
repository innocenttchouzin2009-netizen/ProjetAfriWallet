using System.Text;
using AfriWallet.Ledger.Domain;
using AfriWallet.Ledger.Persistence;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.PaymentRequests.Persistence;
using AfriWallet.Timeline.Application;
using AfriWallet.Timeline.Domain;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Timeline.Infrastructure;

public sealed class AggregatingFinancialTimelineReader : IFinancialTimelineReader
{
    private const string TransferReferencePrefix = "TRF-";

    private readonly LedgerDbContext ledgerDbContext;
    private readonly PaymentRequestDbContext paymentRequestDbContext;
    private readonly IWalletRepository walletRepository;
    private readonly IReadOnlyDictionary<Guid, AccountId> ledgerAccountByWallet;

    public AggregatingFinancialTimelineReader(
        LedgerDbContext ledgerDbContext,
        PaymentRequestDbContext paymentRequestDbContext,
        IWalletRepository walletRepository,
        IReadOnlyDictionary<Guid, AccountId> ledgerAccountByWallet)
    {
        this.ledgerDbContext = ledgerDbContext ?? throw new ArgumentNullException(nameof(ledgerDbContext));
        this.paymentRequestDbContext = paymentRequestDbContext ?? throw new ArgumentNullException(nameof(paymentRequestDbContext));
        this.walletRepository = walletRepository ?? throw new ArgumentNullException(nameof(walletRepository));
        this.ledgerAccountByWallet = ledgerAccountByWallet ?? throw new ArgumentNullException(nameof(ledgerAccountByWallet));
    }

    public async Task<FinancialTimelinePage> ReadAsync(
        FinancialTimelineQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        var ownerWallets = await walletRepository.ListByOwnerAsync(query.OwnerId, cancellationToken);
        var selectedWallets = query.WalletId is null
            ? ownerWallets
            : ownerWallets.Where(wallet => wallet.Id == query.WalletId.Value).ToArray();

        if (selectedWallets.Count == 0)
            return FinancialTimelinePage.Create([]);

        var walletById = selectedWallets.ToDictionary(wallet => wallet.Id.Value);
        var accountToWallet = new Dictionary<Guid, Guid>();
        foreach (var wallet in selectedWallets)
        {
            if (ledgerAccountByWallet.TryGetValue(wallet.Id.Value, out var accountId))
                accountToWallet[accountId.Value] = wallet.Id.Value;
        }

        var entries = new List<FinancialTimelineEntry>();
        var ledgerTransferIds = new HashSet<Guid>();

        if (accountToWallet.Count > 0)
        {
            var accountIds = accountToWallet.Keys.ToArray();
            var ledgerLines = await ledgerDbContext.Lines
                .AsNoTracking()
                .Include(line => line.JournalEntry)
                .Where(line => accountIds.Contains(line.AccountId))
                .ToListAsync(cancellationToken);

            foreach (var line in ledgerLines)
            {
                if (!accountToWallet.TryGetValue(line.AccountId, out var walletIdValue))
                    continue;

                var journal = line.JournalEntry;
                var isTransfer = TryParseTransferId(journal.BusinessReference, out var transferId);
                if (isTransfer)
                    ledgerTransferIds.Add(transferId);

                var source = TimelineSourceReference.Create(
                    isTransfer ? "transfer" : "ledger",
                    isTransfer ? transferId.ToString("N") : journal.Id.ToString("N"));

                var direction = line.Side == (int)LedgerSide.Debit
                    ? FinancialTimelineDirection.Outgoing
                    : FinancialTimelineDirection.Incoming;

                entries.Add(FinancialTimelineEntry.Create(
                    query.OwnerId,
                    WalletId.From(walletIdValue),
                    source,
                    isTransfer ? FinancialTimelineEntryKind.MoneyTransfer : FinancialTimelineEntryKind.LedgerPosting,
                    direction,
                    FinancialTimelineState.Completed,
                    Currency.Create(journal.CurrencyCode),
                    line.AmountMinor,
                    journal.PostedAtUtc,
                    journal.PostedAtUtc,
                    isTransfer ? "Money transfer" : "Ledger posting"));
            }
        }

        var walletIds = walletById.Keys.ToArray();
        var paymentRequests = await paymentRequestDbContext.PaymentRequests
            .AsNoTracking()
            .Where(request =>
                walletIds.Contains(request.RequesterWalletId) ||
                (request.AcceptedPayerWalletId.HasValue && walletIds.Contains(request.AcceptedPayerWalletId.Value)))
            .ToListAsync(cancellationToken);

        foreach (var request in paymentRequests)
        {
            if (request.Status == (int)PaymentRequestStatus.Paid &&
                request.TransferId is Guid transferId &&
                ledgerTransferIds.Contains(transferId))
            {
                continue;
            }

            var isRequester = walletById.ContainsKey(request.RequesterWalletId);
            var walletId = isRequester
                ? request.RequesterWalletId
                : request.AcceptedPayerWalletId!.Value;

            var occurredAt = ParseUtc(request.CreatedAtUtc, nameof(request.CreatedAtUtc));
            var updatedAt = ParseUtc(request.UpdatedAtUtc, nameof(request.UpdatedAtUtc));

            entries.Add(FinancialTimelineEntry.Create(
                query.OwnerId,
                WalletId.From(walletId),
                TimelineSourceReference.Create("payment-request", request.Id.ToString("N")),
                FinancialTimelineEntryKind.PaymentRequest,
                isRequester ? FinancialTimelineDirection.Incoming : FinancialTimelineDirection.Outgoing,
                MapState((PaymentRequestStatus)request.Status),
                Currency.Create(request.CurrencyCode),
                request.AmountMinor,
                occurredAt,
                updatedAt,
                isRequester ? "Payment requested" : "Payment request"));
        }

        var deduplicated = entries
            .GroupBy(entry => $"{entry.Source.Key}|{entry.WalletId.Value:N}", StringComparer.Ordinal)
            .Select(group => group
                .OrderByDescending(entry => entry.UpdatedAtUtc)
                .ThenByDescending(entry => entry.OccurredAtUtc)
                .First())
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .ThenByDescending(entry => entry.UpdatedAtUtc)
            .ThenByDescending(entry => entry.Source.Key, StringComparer.Ordinal)
            .ThenByDescending(entry => entry.WalletId.Value)
            .ToList();

        var cursor = TimelineCursor.TryDecode(query.Cursor);
        if (cursor is not null)
        {
            deduplicated = deduplicated
                .Where(entry => TimelineCursor.IsAfter(entry, cursor))
                .ToList();
        }

        var pageItems = deduplicated.Take(query.Limit).ToArray();
        var nextCursor = deduplicated.Count > query.Limit
            ? TimelineCursor.Encode(pageItems[^1])
            : null;

        return FinancialTimelinePage.Create(pageItems, nextCursor);
    }

    private static FinancialTimelineState MapState(PaymentRequestStatus status) => status switch
    {
        PaymentRequestStatus.Pending or PaymentRequestStatus.Accepted => FinancialTimelineState.Pending,
        PaymentRequestStatus.Paid => FinancialTimelineState.Completed,
        PaymentRequestStatus.Declined => FinancialTimelineState.Declined,
        PaymentRequestStatus.Expired => FinancialTimelineState.Expired,
        PaymentRequestStatus.Cancelled => FinancialTimelineState.Cancelled,
        _ => throw new InvalidOperationException("Unsupported payment request status for timeline projection.")
    };

    private static bool TryParseTransferId(string businessReference, out Guid transferId)
    {
        transferId = Guid.Empty;
        return !string.IsNullOrWhiteSpace(businessReference) &&
               businessReference.StartsWith(TransferReferencePrefix, StringComparison.Ordinal) &&
               Guid.TryParse(businessReference[TransferReferencePrefix.Length..], out transferId) &&
               transferId != Guid.Empty;
    }

    private static DateTimeOffset ParseUtc(string value, string fieldName)
    {
        if (!DateTimeOffset.TryParse(value, out var parsed) || parsed.Offset != TimeSpan.Zero)
            throw new InvalidOperationException($"Persisted {fieldName} is not a valid UTC timestamp.");
        return parsed;
    }

    private sealed record TimelineCursor(long OccurredTicks, long UpdatedTicks, string SourceKey, Guid WalletId)
    {
        public static string Encode(FinancialTimelineEntry entry)
        {
            var raw = $"{entry.OccurredAtUtc.UtcTicks}|{entry.UpdatedAtUtc.UtcTicks}|{entry.Source.Key}|{entry.WalletId.Value:N}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        }

        public static TimelineCursor? TryDecode(string? encoded)
        {
            if (encoded is null)
                return null;

            try
            {
                var raw = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                var parts = raw.Split('|', 4);
                if (parts.Length != 4 ||
                    !long.TryParse(parts[0], out var occurredTicks) ||
                    !long.TryParse(parts[1], out var updatedTicks) ||
                    string.IsNullOrWhiteSpace(parts[2]) ||
                    !Guid.TryParseExact(parts[3], "N", out var walletId))
                {
                    throw new FormatException();
                }

                return new TimelineCursor(occurredTicks, updatedTicks, parts[2], walletId);
            }
            catch (Exception exception) when (exception is FormatException or ArgumentException)
            {
                throw new ArgumentException("Timeline cursor is invalid.", nameof(encoded));
            }
        }

        public static bool IsAfter(FinancialTimelineEntry entry, TimelineCursor cursor)
        {
            if (entry.OccurredAtUtc.UtcTicks != cursor.OccurredTicks)
                return entry.OccurredAtUtc.UtcTicks < cursor.OccurredTicks;
            if (entry.UpdatedAtUtc.UtcTicks != cursor.UpdatedTicks)
                return entry.UpdatedAtUtc.UtcTicks < cursor.UpdatedTicks;

            var sourceCompare = string.CompareOrdinal(entry.Source.Key, cursor.SourceKey);
            if (sourceCompare != 0)
                return sourceCompare < 0;

            return entry.WalletId.Value.CompareTo(cursor.WalletId) < 0;
        }
    }
}
