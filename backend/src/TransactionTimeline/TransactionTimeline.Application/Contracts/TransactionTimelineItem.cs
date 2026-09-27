namespace AfriWallet.TransactionTimeline.Application.Contracts;

public sealed record TransactionTimelineItem(
    string TransactionId,
    long AmountMinor,
    string CurrencyCode,
    TransactionTimelineDirection Direction,
    TransactionTimelineStatus Status,
    DateTimeOffset OccurredAt,
    string Reference,
    string? CounterpartyLabel);
