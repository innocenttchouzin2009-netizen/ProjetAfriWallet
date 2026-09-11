namespace IdentityService.Api.PaymentRequests;

public sealed record CreatePaymentRequestHttpRequest(
    Guid RequesterWalletId,
    string RecipientKind,
    string RecipientValue,
    string CurrencyCode,
    long AmountMinor,
    Guid CorrelationId,
    DateTimeOffset? ExpiresAtUtc);

public sealed record PaymentRequestHttpResponse(
    Guid Id,
    Guid RequesterWalletId,
    string RecipientKind,
    string CurrencyCode,
    long AmountMinor,
    Guid CorrelationId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    string Status,
    Guid? AcceptedPayerWalletId,
    DateTimeOffset? AcceptedAtUtc,
    Guid? TransferId,
    DateTimeOffset? ClosedAtUtc);

public sealed record PaymentRequestErrorResponse(string Code, string Message, string TraceId);

public static class PaymentRequestErrorCode
{
    public const string Unauthorized = "PAYMENT_REQUEST_UNAUTHORIZED";
    public const string ValidationError = "PAYMENT_REQUEST_VALIDATION_ERROR";
    public const string NotFound = "PAYMENT_REQUEST_NOT_FOUND";
    public const string RecipientNotFound = "PAYMENT_REQUEST_RECIPIENT_NOT_FOUND";
    public const string Conflict = "PAYMENT_REQUEST_CONFLICT";
}
