using System.Text.Json;
using System.Text.Json.Serialization;
using AfriWallet.Webhooks.Domain;

namespace IdentityService.Api.Webhooks;

public sealed record RegisterWebhookSubscriptionRequest(
    string Endpoint,
    IReadOnlyList<string> EventTypes,
    string SigningKeyReference)
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}

public sealed record WebhookSubscriptionResponse(
    Guid Id,
    string Endpoint,
    IReadOnlyList<string> EventTypes,
    string SigningKeyReference,
    WebhookSubscriptionStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record WebhookManagementErrorResponse(
    string Code,
    string Message,
    string TraceId);

public static class WebhookManagementErrorCode
{
    public const string Unauthorized = "WEBHOOK_MGMT_UNAUTHORIZED";
    public const string ValidationError = "WEBHOOK_MGMT_VALIDATION_ERROR";
    public const string NotFound = "WEBHOOK_MGMT_NOT_FOUND";
    public const string Conflict = "WEBHOOK_MGMT_CONFLICT";
}
