using AfriWallet.Timeline.Domain;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.Timeline.Application;

public sealed record FinancialTimelineQuery
{
    private FinancialTimelineQuery(Guid ownerId, WalletId? walletId, int limit, string? cursor)
    {
        OwnerId = ownerId;
        WalletId = walletId;
        Limit = limit;
        Cursor = cursor;
    }

    public Guid OwnerId { get; }
    public WalletId? WalletId { get; }
    public int Limit { get; }
    public string? Cursor { get; }

    public static FinancialTimelineQuery Create(Guid ownerId, WalletId? walletId = null, int limit = 50, string? cursor = null)
    {
        if (ownerId == Guid.Empty)
            throw new ArgumentException("Owner id cannot be empty.", nameof(ownerId));
        if (walletId is not null && walletId.Value.Value == Guid.Empty)
            throw new ArgumentException("Wallet id cannot be empty.", nameof(walletId));
        if (limit is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(limit), "Timeline limit must be between 1 and 100.");

        var normalizedCursor = string.IsNullOrWhiteSpace(cursor) ? null : cursor.Trim();
        if (normalizedCursor is not null && normalizedCursor.Length > 512)
            throw new ArgumentException("Timeline cursor cannot exceed 512 characters.", nameof(cursor));

        return new FinancialTimelineQuery(ownerId, walletId, limit, normalizedCursor);
    }
}

public sealed record FinancialTimelinePage(
    IReadOnlyList<FinancialTimelineEntry> Items,
    string? NextCursor)
{
    public static FinancialTimelinePage Create(IReadOnlyList<FinancialTimelineEntry> items, string? nextCursor = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Any(item => item is null))
            throw new ArgumentException("Timeline page cannot contain null entries.", nameof(items));

        var normalizedCursor = string.IsNullOrWhiteSpace(nextCursor) ? null : nextCursor.Trim();
        if (normalizedCursor is not null && normalizedCursor.Length > 512)
            throw new ArgumentException("Next cursor cannot exceed 512 characters.", nameof(nextCursor));

        return new FinancialTimelinePage(items, normalizedCursor);
    }
}

public interface IFinancialTimelineReader
{
    Task<FinancialTimelinePage> ReadAsync(
        FinancialTimelineQuery query,
        CancellationToken cancellationToken = default);
}
