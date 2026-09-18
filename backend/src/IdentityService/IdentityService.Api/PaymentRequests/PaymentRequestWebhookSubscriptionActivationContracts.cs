namespace IdentityService.Api.PaymentRequests;

public sealed record PaymentRequestWebhookSubscriptionActivationResponse(
    Guid Id,
    string IntegrationId,
    Guid? MerchantId,
    string EndpointUrl,
    string Status,
    string KeyId,
    string SecretReference,
    IReadOnlyList<string> EventTypes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PaymentRequestWebhookSubscriptionActivationErrorResponse(
    string Code,
    string Message,
    string TraceId);

public static class PaymentRequestWebhookSubscriptionActivationErrorCode
{
    public const string Forbidden = "WEBHOOK_SUBSCRIPTION_FORBIDDEN";
    public const string ValidationError = "WEBHOOK_SUBSCRIPTION_VALIDATION_ERROR";
    public const string NotFound = "WEBHOOK_SUBSCRIPTION_NOT_FOUND";
    public const string Conflict = "WEBHOOK_SUBSCRIPTION_CONFLICT";
}
