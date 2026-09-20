using System.Globalization;
using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.Persistence;

public sealed class EfPaymentRequestRepository(PaymentRequestDbContext dbContext)
    : IPaymentRequestRepository, IPaymentRequestQueryRepository
{
    public async Task<PaymentRequest?> GetAsync(
        PaymentRequestId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entity = await dbContext.PaymentRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id.Value, cancellationToken);
        return entity is null ? null : PaymentRequestEntityMapper.ToDomain(entity);
    }

    public async Task<PaymentRequest?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException("Correlation id cannot be empty.", nameof(correlationId));
        }

        var entity = await dbContext.PaymentRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.CorrelationId == correlationId, cancellationToken);
        return entity is null ? null : PaymentRequestEntityMapper.ToDomain(entity);
    }

    public async Task AddAsync(
        PaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        dbContext.PaymentRequests.Add(PaymentRequestEntityMapper.ToEntity(request));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            dbContext.ChangeTracker.Clear();
            throw new InvalidOperationException(
                "Payment request id or correlation id is already persisted.",
                exception);
        }
    }

    public async Task UpdateAsync(
        PaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await dbContext.PaymentRequests
            .SingleOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException("Payment request was not found.");
        }

        PaymentRequestEntityMapper.Apply(entity, request);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            dbContext.ChangeTracker.Clear();
            throw new InvalidOperationException(
                "Payment request persistence update violated an idempotency constraint.",
                exception);
        }
    }

    public async Task<PaymentRequestQueryPage> ListReceivedAsync(
        ReceivedPaymentRequestsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ValidatePage(query.Page);
        ValidateOrder(query.Order);
        cancellationToken.ThrowIfCancellationRequested();

        var references = query.RecipientReferences ?? throw new ArgumentNullException(nameof(query.RecipientReferences));
        var walletIds = query.RecipientWalletIds ?? throw new ArgumentNullException(nameof(query.RecipientWalletIds));
        if (references.Count == 0 && walletIds.Count == 0)
        {
            throw new ArgumentException("At least one recipient reference or wallet id is required.", nameof(query));
        }

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
        var recipientWalletIds = walletIds.Select(id => id.Value).Distinct().ToArray();

        IQueryable<PaymentRequestEntity> source = dbContext.PaymentRequests.AsNoTracking();
        source = source.Where(entity =>
            (entity.PayerReferenceKind == (int)RecipientReferenceKind.AfWalId && afWalIds.Contains(entity.PayerReferenceValue)) ||
            (entity.PayerReferenceKind == (int)RecipientReferenceKind.QrToken && qrTokens.Contains(entity.PayerReferenceValue)) ||
            (entity.AcceptedPayerWalletId.HasValue && recipientWalletIds.Contains(entity.AcceptedPayerWalletId.Value)));

        source = ApplyStatusFilter(source, query.Statuses);
        return await ToPageAsync(source, query.Page, query.Order, cancellationToken);
    }

    public async Task<PaymentRequestQueryPage> ListSentAsync(
        SentPaymentRequestsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ValidatePage(query.Page);
        ValidateOrder(query.Order);
        cancellationToken.ThrowIfCancellationRequested();

        var requesterWalletIds = query.RequesterWalletIds ?? throw new ArgumentNullException(nameof(query.RequesterWalletIds));
        if (requesterWalletIds.Count == 0)
        {
            throw new ArgumentException("At least one requester wallet id is required.", nameof(query));
        }

        var walletIds = requesterWalletIds.Select(id => id.Value).Distinct().ToArray();
        IQueryable<PaymentRequestEntity> source = dbContext.PaymentRequests
            .AsNoTracking()
            .Where(entity => walletIds.Contains(entity.RequesterWalletId));

        source = ApplyStatusFilter(source, query.Statuses);
        return await ToPageAsync(source, query.Page, query.Order, cancellationToken);
    }

    private static IQueryable<PaymentRequestEntity> ApplyStatusFilter(
        IQueryable<PaymentRequestEntity> source,
        IReadOnlyCollection<PaymentRequestStatus>? statuses)
    {
        if (statuses is null || statuses.Count == 0)
        {
            return source;
        }

        var values = statuses.Select(status => (int)status).Distinct().ToArray();
        return source.Where(entity => values.Contains(entity.Status));
    }

    private static async Task<PaymentRequestQueryPage> ToPageAsync(
        IQueryable<PaymentRequestEntity> source,
        PaymentRequestPageRequest page,
        PaymentRequestTemporalOrder order,
        CancellationToken cancellationToken)
    {
        var totalCount = await source.LongCountAsync(cancellationToken);
        var skipLong = checked((long)page.PageNumber * page.PageSize);
        if (skipLong > int.MaxValue)
        {
            return new PaymentRequestQueryPage([], page.PageNumber, page.PageSize, totalCount, false);
        }

        IOrderedQueryable<PaymentRequestEntity> ordered = order switch
        {
            PaymentRequestTemporalOrder.NewestFirst => source
                .OrderByDescending(entity => entity.CreatedAtUtc)
                .ThenByDescending(entity => entity.Id),
            PaymentRequestTemporalOrder.OldestFirst => source
                .OrderBy(entity => entity.CreatedAtUtc)
                .ThenBy(entity => entity.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(order))
        };

        var entities = await ordered
            .Skip((int)skipLong)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        var items = entities.Select(ToQueryItem).ToArray();
        var consumed = skipLong + items.Length;
        return new PaymentRequestQueryPage(
            items,
            page.PageNumber,
            page.PageSize,
            totalCount,
            consumed < totalCount);
    }

    private static PaymentRequestQueryItem ToQueryItem(PaymentRequestEntity entity) => new(
        PaymentRequestId.From(entity.Id),
        WalletId.From(entity.RequesterWalletId),
        (RecipientReferenceKind)entity.PayerReferenceKind,
        Currency.Create(entity.CurrencyCode),
        entity.AmountMinor,
        entity.CorrelationId,
        ParseRequired(entity.CreatedAtUtc, nameof(entity.CreatedAtUtc)),
        ParseOptional(entity.ExpiresAtUtc),
        ParseRequired(entity.UpdatedAtUtc, nameof(entity.UpdatedAtUtc)),
        (PaymentRequestStatus)entity.Status,
        entity.AcceptedPayerWalletId is null ? null : WalletId.From(entity.AcceptedPayerWalletId.Value),
        ParseOptional(entity.AcceptedAtUtc),
        entity.TransferId,
        ParseOptional(entity.ClosedAtUtc));

    private static void ValidatePage(PaymentRequestPageRequest page)
    {
        ArgumentNullException.ThrowIfNull(page);
        _ = PaymentRequestPageRequest.Create(page.PageNumber, page.PageSize);
    }

    private static void ValidateOrder(PaymentRequestTemporalOrder order)
    {
        if (order is not (PaymentRequestTemporalOrder.NewestFirst or PaymentRequestTemporalOrder.OldestFirst))
        {
            throw new ArgumentOutOfRangeException(nameof(order));
        }
    }

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
}
