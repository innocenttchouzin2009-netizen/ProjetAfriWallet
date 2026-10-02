namespace IdentityService.Api.TransactionHistory;

public sealed record TransactionHistoryItemResponse(
    Guid TransactionId,
    Guid WalletId,
    long AmountMinor,
    string CurrencyCode,
    string Direction,
    string Status,
    DateTimeOffset OccurredAtUtc,
    string Reference,
    string? CounterpartyLabel);

public sealed record TransactionHistoryPageResponse(
    IReadOnlyList<TransactionHistoryItemResponse> Items,
    string? NextCursor);

public sealed record TransactionHistoryErrorResponse(
    string Code,
    string Message,
    string? TraceId = null);

public static class TransactionHistoryHttpErrorCode
{
    public const string Unauthorized = "TRANSACTION_HISTORY_UNAUTHORIZED";
    public const string ValidationError = "TRANSACTION_HISTORY_VALIDATION_ERROR";
}
