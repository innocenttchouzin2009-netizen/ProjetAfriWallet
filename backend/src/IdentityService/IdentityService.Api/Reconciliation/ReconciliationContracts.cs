namespace IdentityService.Api.Reconciliation;

public static class ReconciliationErrorCode
{
    public const string Unauthorized = "RECON_UNAUTHORIZED";
    public const string NotFound = "RECON_NOT_FOUND";
    public const string InvalidState = "RECON_INVALID_STATE";
    public const string ValidationError = "RECON_VALIDATION_ERROR";
}

public sealed record ReconciliationErrorResponse(string Code, string Message, string TraceId);

public sealed record ReconciliationTransferResponse(
    Guid TransferId,
    Guid JournalEntryId,
    Guid CorrelationId,
    Guid SourceWalletId,
    Guid TargetWalletId,
    string CurrencyCode,
    long AmountMinor,
    DateTimeOffset PostedAtUtc,
    bool SourceWalletActive,
    bool TargetWalletActive);
