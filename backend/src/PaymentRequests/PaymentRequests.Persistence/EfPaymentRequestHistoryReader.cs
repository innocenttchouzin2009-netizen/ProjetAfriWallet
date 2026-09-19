using System.Globalization;
using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.Persistence;

public sealed class EfPaymentRequestHistoryReader(PaymentRequestDbContext dbContext)
    : IPaymentRequestHistoryReader
{
    public async Task<PaymentRequestHistoryPage> ListAsync(
        PaymentRequestHistoryReadQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.Page);
        _ = PaymentRequestPageRequest.Create(query.Page.PageNumber, query.Page.PageSize);
        cancellationToken.ThrowIfCancellationRequested();

        if (!Enum.IsDefined(query.Direction))
        {
            throw new ArgumentOutOfRangeException(nameof(query.Direction));
        }

        if (query.Order is not (PaymentRequestTemporalOrder.NewestFirst or PaymentRequestTemporalOrder.OldestFirst))
        {
            throw new ArgumentOutOfRangeException(nameof(query.Order));
        }

        if (query.Statuses is not null && query.Statuses.Any(status => !Enum.IsDefined(status)))
        {
            throw new ArgumentOutOfRangeException(nameof(query.Statuses));
        }

        var ownedWalletIds = (query.OwnedWalletIds ?? throw new ArgumentNullException(nameof(query.OwnedWalletIds)))
            .Where(wallet => wallet.Value != Guid.Empty)
            .Select(wallet => wallet.Value)
            .Distinct()
            .ToArray();

        var references = (query.OwnedRecipientReferences ?? throw new ArgumentNullException(nameof(query.OwnedRecipientReferences)))
            .Where(reference => reference is not null)
            .Distinct()
            .ToArray();

        var afWalIds = references
            .Where(reference => reference.Kind == RecipientReferenceKind.AfWalId)
            .Select(reference => reference.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var qrTokens = references
            .Where(reference => reference.Kind == RecipientReferenceKind.QrToken)
            .Select(reference => reference.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var statuses = query.Statuses is null || query.Statuses.Count == 0
            ? null
            : query.Statuses.Select(status => (int)status).Distinct().ToArray();

        var baseQuery = dbContext.PaymentRequests.AsNoTracking();

        IQueryable<HistoryRow>? source = query.Direction switch
        {
            PaymentRequestHistoryDirection.Sent => BuildSent(baseQuery, ownedWalletIds, statuses),
            PaymentRequestHistoryDirection.Received => BuildReceived(baseQuery, ownedWalletIds, afWalIds, qrTokens, statuses),
            PaymentRequestHistoryDirection.All => BuildAll(baseQuery, ownedWalletIds, afWalIds, qrTokens, statuses),
            _ => throw new ArgumentOutOfRangeException(nameof(query.Direction))
        };

        if (source is null)
        {
            return Empty(query.Page);
        }

        var totalCount = await source.LongCountAsync(cancellationToken);
        var skipLong = checked((long)query.Page.PageNumber * query.Page.PageSize);
        if (skipLong > int.MaxValue)
        {
            return new PaymentRequestHistoryPage([], query.Page.PageNumber, query.Page.PageSize, totalCount, false);
        }

        IOrderedQueryable<HistoryRow> ordered = query.Order switch
        {
            PaymentRequestTemporalOrder.NewestFirst => source
                .OrderByDescending(row => row.CreatedAtUtc)
                .ThenByDescending(row => row.Id)
                .ThenByDescending(row => row.Direction),
            PaymentRequestTemporalOrder.OldestFirst => source
                .OrderBy(row => row.CreatedAtUtc)
                .ThenBy(row => row.Id)
                .ThenBy(row => row.Direction),
            _ => throw new ArgumentOutOfRangeException(nameof(query.Order))
        };

        var rows = await ordered
            .Skip((int)skipLong)
            .Take(query.Page.PageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(ToHistoryItem).ToArray();
        var consumed = skipLong + items.Length;
        return new PaymentRequestHistoryPage(
            items,
            query.Page.PageNumber,
            query.Page.PageSize,
            totalCount,
            consumed < totalCount);
    }

    private static IQueryable<HistoryRow>? BuildSent(
        IQueryable<PaymentRequestEntity> baseQuery,
        Guid[] ownedWalletIds,
        int[]? statuses)
    {
        if (ownedWalletIds.Length == 0)
        {
            return null;
        }

        var source = baseQuery.Where(entity => ownedWalletIds.Contains(entity.RequesterWalletId));
        source = ApplyStatusFilter(source, statuses);
        return Project(source, PaymentRequestHistoryDirection.Sent);
    }

    private static IQueryable<HistoryRow>? BuildReceived(
        IQueryable<PaymentRequestEntity> baseQuery,
        Guid[] ownedWalletIds,
        string[] afWalIds,
        string[] qrTokens,
        int[]? statuses)
    {
        if (ownedWalletIds.Length == 0 && afWalIds.Length == 0 && qrTokens.Length == 0)
        {
            return null;
        }

        var source = baseQuery.Where(entity =>
            (entity.PayerReferenceKind == (int)RecipientReferenceKind.AfWalId && afWalIds.Contains(entity.PayerReferenceValue)) ||
            (entity.PayerReferenceKind == (int)RecipientReferenceKind.QrToken && qrTokens.Contains(entity.PayerReferenceValue)) ||
            (entity.AcceptedPayerWalletId.HasValue && ownedWalletIds.Contains(entity.AcceptedPayerWalletId.Value)));
        source = ApplyStatusFilter(source, statuses);
        return Project(source, PaymentRequestHistoryDirection.Received);
    }

    private static IQueryable<HistoryRow>? BuildAll(
        IQueryable<PaymentRequestEntity> baseQuery,
        Guid[] ownedWalletIds,
        string[] afWalIds,
        string[] qrTokens,
        int[]? statuses)
    {
        var sent = BuildSent(baseQuery, ownedWalletIds, statuses);
        var received = BuildReceived(baseQuery, ownedWalletIds, afWalIds, qrTokens, statuses);

        return (sent, received) switch
        {
            (null, null) => null,
            (not null, null) => sent,
            (null, not null) => received,
            _ => sent!.Concat(received!)
        };
    }

    private static IQueryable<PaymentRequestEntity> ApplyStatusFilter(
        IQueryable<PaymentRequestEntity> source,
        int[]? statuses) =>
        statuses is null ? source : source.Where(entity => statuses.Contains(entity.Status));

    private static IQueryable<HistoryRow> Project(
        IQueryable<PaymentRequestEntity> source,
        PaymentRequestHistoryDirection direction) =>
        source.Select(entity => new HistoryRow
        {
            Id = entity.Id,
            RequesterWalletId = entity.RequesterWalletId,
            PayerReferenceKind = entity.PayerReferenceKind,
            CurrencyCode = entity.CurrencyCode,
            AmountMinor = entity.AmountMinor,
            CorrelationId = entity.CorrelationId,
            CreatedAtUtc = entity.CreatedAtUtc,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            Status = entity.Status,
            AcceptedPayerWalletId = entity.AcceptedPayerWalletId,
            AcceptedAtUtc = entity.AcceptedAtUtc,
            TransferId = entity.TransferId,
            ClosedAtUtc = entity.ClosedAtUtc,
            Direction = (int)direction
        });

    private static PaymentRequestHistoryItem ToHistoryItem(HistoryRow row) => new(
        new PaymentRequestQueryItem(
            PaymentRequestId.From(row.Id),
            WalletId.From(row.RequesterWalletId),
            (RecipientReferenceKind)row.PayerReferenceKind,
            Currency.Create(row.CurrencyCode),
            row.AmountMinor,
            row.CorrelationId,
            ParseRequired(row.CreatedAtUtc, nameof(row.CreatedAtUtc)),
            ParseOptional(row.ExpiresAtUtc),
            ParseRequired(row.UpdatedAtUtc, nameof(row.UpdatedAtUtc)),
            (PaymentRequestStatus)row.Status,
            row.AcceptedPayerWalletId is null ? null : WalletId.From(row.AcceptedPayerWalletId.Value),
            ParseOptional(row.AcceptedAtUtc),
            row.TransferId,
            ParseOptional(row.ClosedAtUtc)),
        (PaymentRequestHistoryDirection)row.Direction);

    private static PaymentRequestHistoryPage Empty(PaymentRequestPageRequest page) =>
        new([], page.PageNumber, page.PageSize, 0, false);

    private static DateTimeOffset ParseRequired(string value, string fieldName)
    {
        if (!DateTimeOffset.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            throw new InvalidOperationException($"Persisted {fieldName} is invalid.");
        }

        return parsed;
    }

    private static DateTimeOffset? ParseOptional(string? value)
    {
        if (value is null)
        {
            return null;
        }

        if (!DateTimeOffset.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            throw new InvalidOperationException("Persisted payment request timestamp is invalid.");
        }

        return parsed;
    }

    private sealed class HistoryRow
    {
        public Guid Id { get; init; }
        public Guid RequesterWalletId { get; init; }
        public int PayerReferenceKind { get; init; }
        public string CurrencyCode { get; init; } = string.Empty;
        public long AmountMinor { get; init; }
        public Guid CorrelationId { get; init; }
        public string CreatedAtUtc { get; init; } = string.Empty;
        public string? ExpiresAtUtc { get; init; }
        public string UpdatedAtUtc { get; init; } = string.Empty;
        public int Status { get; init; }
        public Guid? AcceptedPayerWalletId { get; init; }
        public string? AcceptedAtUtc { get; init; }
        public Guid? TransferId { get; init; }
        public string? ClosedAtUtc { get; init; }
        public int Direction { get; init; }
    }
}
