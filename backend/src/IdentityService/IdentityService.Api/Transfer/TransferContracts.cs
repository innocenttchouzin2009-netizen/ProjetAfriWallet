namespace IdentityService.Api.Transfer;

public sealed record InternalTransferRequest(
    Guid SourceWalletId,
    Guid TargetWalletId,
    long AmountMinor,
    Guid CorrelationId);

public sealed record InternalTransferResponse(
    Guid TransferId,
    Guid SourceWalletId,
    Guid TargetWalletId,
    string CurrencyCode,
    long AmountMinor,
    Guid CorrelationId,
    Guid JournalEntryId,
    DateTimeOffset PostedAtUtc);

public sealed record TransferErrorResponse(string Code, string Message, string TraceId);

public static class TransferErrorCode
{
    public const string Unauthorized = "TRANSFER_UNAUTHORIZED";
    public const string ValidationError = "TRANSFER_VALIDATION_ERROR";
    public const string NotFound = "TRANSFER_NOT_FOUND";
    public const string Conflict = "TRANSFER_CONFLICT";
}
