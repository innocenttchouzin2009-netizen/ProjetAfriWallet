using System.Text.Json;
using System.Text.Json.Serialization;

namespace IdentityService.Api.PaymentRequests;

public sealed record CreatePaymentRequestWebhookSubscriptionRequest(
    string EndpointUrl,
    string KeyId,
    string SecretReference,
    IReadOnlyList<string> EventTypes)
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}

public sealed record UpdatePaymentRequestWebhookSubscriptionRequest(
    string EndpointUrl,
    string KeyId,
    string SecretReference,
    IReadOnlyList<string> EventTypes)
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}

public sealed record PaymentRequestWebhookSubscriptionResponse(
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

public sealed record PaymentRequestWebhookSubscriptionErrorResponse(
    string Code,
    string Message,
    string TraceId);

public static class PaymentRequestWebhookSubscriptionErrorCode
{
    public const string Unauthorized = "WEBHOOK_SUBSCRIPTION_UNAUTHORIZED";
    public const string Forbidden = "WEBHOOK_SUBSCRIPTION_FORBIDDEN";
    public const string ValidationError = "WEBHOOK_SUBSCRIPTION_VALIDATION_ERROR";
    public const string NotFound = "WEBHOOK_SUBSCRIPTION_NOT_FOUND";
    public const string Conflict = "WEBHOOK_SUBSCRIPTION_CONFLICT";
}
