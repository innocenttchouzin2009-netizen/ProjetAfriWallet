using AfriWallet.TransactionTimeline.Application.Contracts;

namespace AfriWallet.TransactionTimeline.Application.Projection;

public sealed record TransactionTimelineProjectionEntry(
    Guid WalletId,
    string TransactionId,
    long AmountMinor,
    string CurrencyCode,
    TransactionTimelineDirection Direction,
    TransactionTimelineStatus Status,
    DateTimeOffset OccurredAt,
    string Reference,
    string? CounterpartyLabel);
