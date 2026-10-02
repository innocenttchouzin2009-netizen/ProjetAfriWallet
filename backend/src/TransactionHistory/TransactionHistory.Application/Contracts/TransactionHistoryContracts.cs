using AfriWallet.TransactionHistory.Application.Cursor;
using AfriWallet.Wallet.Domain;

namespace AfriWallet.TransactionHistory.Application.Contracts;

public enum TransactionHistoryDirection
{
    Incoming,
    Outgoing
}

public enum TransactionHistoryStatus
{
    Pending,
    Completed,
    Failed,
    Cancelled,
    Reversed
}

public sealed record TransactionHistoryItem(
    Guid TransactionId,
    WalletId WalletId,
    long AmountMinor,
    string CurrencyCode,
    TransactionHistoryDirection Direction,
    TransactionHistoryStatus Status,
    DateTimeOffset OccurredAtUtc,
    string Reference,
    string? CounterpartyLabel);

public sealed record TransactionHistoryQuery(
    Guid UserId,
    TransactionHistoryPageRequest Page);

public sealed record TransactionHistoryPage(
    IReadOnlyList<TransactionHistoryItem> Items,
    TransactionHistoryCursor? NextCursor);
