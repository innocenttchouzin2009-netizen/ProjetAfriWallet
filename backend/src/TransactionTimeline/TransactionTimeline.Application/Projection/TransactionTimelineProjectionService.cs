using AfriWallet.TransactionTimeline.Application.Contracts;

namespace AfriWallet.TransactionTimeline.Application.Projection;

public sealed class TransactionTimelineProjectionService
{
    public TransactionTimelinePage Project(
        TransactionTimelineReadRequest request,
        IEnumerable<TransactionTimelineProjectionEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(entries);

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

        var beforeUtc = request.Before?.ToUniversalTime();

        var candidates = entries
            .Where(entry => entry.WalletId == request.WalletId)
            .Where(entry => beforeUtc is null || entry.OccurredAt.ToUniversalTime() < beforeUtc.Value)
            .OrderByDescending(entry => entry.OccurredAt)
            .ThenByDescending(entry => entry.TransactionId, StringComparer.Ordinal)
            .Take(request.Limit + 1)
            .ToArray();

        var hasMore = candidates.Length > request.Limit;
        var items = candidates
            .Take(request.Limit)
            .Select(ProjectItem)
            .ToArray();

        var nextBefore = hasMore && items.Length > 0
            ? items[^1].OccurredAt
            : null;

        return new TransactionTimelinePage(items, hasMore, nextBefore);
    }

    private static TransactionTimelineItem ProjectItem(TransactionTimelineProjectionEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.TransactionId))
        {
            throw new InvalidOperationException("TransactionId is required for timeline projection.");
        }

        if (entry.AmountMinor < 0)
        {
            throw new InvalidOperationException("Timeline amount must use a non-negative minor-unit magnitude.");
        }

        var currencyCode = entry.CurrencyCode.Trim().ToUpperInvariant();
        if (currencyCode.Length != 3)
        {
            throw new InvalidOperationException("Timeline currency code must contain exactly three characters.");
        }

        var reference = entry.Reference.Trim();
        if (reference.Length == 0)
        {
            throw new InvalidOperationException("Timeline reference is required.");
        }

        var counterpartyLabel = string.IsNullOrWhiteSpace(entry.CounterpartyLabel)
            ? null
            : entry.CounterpartyLabel.Trim();

        return new TransactionTimelineItem(
            entry.TransactionId.Trim(),
            entry.AmountMinor,
            currencyCode,
            entry.Direction,
            entry.Status,
            entry.OccurredAt.ToUniversalTime(),
            reference,
            counterpartyLabel);
    }
}
