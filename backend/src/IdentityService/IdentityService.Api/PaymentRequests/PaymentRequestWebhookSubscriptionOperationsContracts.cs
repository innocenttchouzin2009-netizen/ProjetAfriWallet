using System.Text.Json;
using System.Text.Json.Serialization;

namespace IdentityService.Api.PaymentRequests;

public sealed record RotatePaymentRequestWebhookSubscriptionRequest(
    string KeyId,
    string SecretReference)
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}

public sealed record PaymentRequestWebhookConnectivityTestResponse(
    Guid SubscriptionId,
    bool Succeeded,
    int? HttpStatusCode,
    string Detail,
    DateTimeOffset ObservedAtUtc);

public sealed record PaymentRequestWebhookSubscriptionAuditResponse(
    Guid Id,
    string Operation,
    string ActorSubject,
    string KeyId,
    string SecretReference,
    bool Succeeded,
    int? HttpStatusCode,
    string? Detail,
    DateTimeOffset OccurredAtUtc);

public sealed record PaymentRequestWebhookDeliveryHealthResponse(
    Guid SubscriptionId,
    string SubscriptionStatus,
    DateTimeOffset? LastConnectivityTestAtUtc,
    bool? LastConnectivitySucceeded,
    int? LastConnectivityHttpStatusCode,
    string Health);
