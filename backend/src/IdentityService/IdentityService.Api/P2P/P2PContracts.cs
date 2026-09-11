namespace IdentityService.Api.P2P;

public sealed record P2PTransferRequest(
    Guid SourceWalletId,
    string RecipientKind,
    string RecipientValue,
    string CurrencyCode,
    long AmountMinor,
    Guid CorrelationId);

public sealed record P2PTransferResponse(
    Guid TransferId,
    Guid SourceWalletId,
    Guid TargetWalletId,
    string CurrencyCode,
    long AmountMinor,
    Guid CorrelationId,
    DateTimeOffset CreatedAtUtc,
    string RecipientKind);

public sealed record P2PErrorResponse(string Code, string Message, string TraceId);

public static class P2PErrorCode
{
    public const string Unauthorized = "P2P_UNAUTHORIZED";
    public const string ValidationError = "P2P_VALIDATION_ERROR";
    public const string NotFound = "P2P_NOT_FOUND";
    public const string RecipientNotFound = "P2P_RECIPIENT_NOT_FOUND";
    public const string Conflict = "P2P_CONFLICT";
    public const string RecipientProvidersUnavailable = "P2P_RECIPIENT_PROVIDERS_UNAVAILABLE";
}
