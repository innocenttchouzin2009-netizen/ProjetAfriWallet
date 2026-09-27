namespace AfriWallet.TransactionTimeline.Application.Contracts;

public sealed record TransactionTimelinePage(
    IReadOnlyList<TransactionTimelineItem> Items,
    bool HasMore,
    DateTimeOffset? NextBefore);
