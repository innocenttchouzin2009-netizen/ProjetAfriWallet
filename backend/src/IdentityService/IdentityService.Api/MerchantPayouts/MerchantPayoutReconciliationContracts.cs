using AfriWallet.Merchants.Payout.Domain;

namespace IdentityService.Api.MerchantPayouts;

public sealed record ReconcileMerchantPayoutProviderResultRequest(
    Guid ResultId,
    MerchantPayoutProviderResultStatus Status,
    string? ProviderReference,
    string? FailureCode,
    long AmountMinor,
    string Currency,
    DateTimeOffset ObservedAtUtc);

public sealed record MerchantPayoutReconciliationResponse(
    Guid ReconciliationId,
    Guid PayoutId,
    Guid ResultId,
    string MerchantId,
    string Status,
    string ReasonCode,
    DateTimeOffset EvaluatedAtUtc);

public sealed record MerchantPayoutReconciliationErrorResponse(
    string Code,
    string Message,
    string TraceId);

public static class MerchantPayoutReconciliationErrorCode
{
    public const string Forbidden = "MERCHANT_PAYOUT_RECONCILIATION_FORBIDDEN";
    public const string ValidationError = "MERCHANT_PAYOUT_RECONCILIATION_VALIDATION_ERROR";
    public const string NotFound = "MERCHANT_PAYOUT_RECONCILIATION_NOT_FOUND";
    public const string Conflict = "MERCHANT_PAYOUT_RECONCILIATION_CONFLICT";
}
