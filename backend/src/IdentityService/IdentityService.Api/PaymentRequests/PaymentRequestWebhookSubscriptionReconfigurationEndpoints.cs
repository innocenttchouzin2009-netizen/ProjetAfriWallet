using System.Security.Claims;
using AfriWallet.PaymentRequests.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.PaymentRequests;

public static class PaymentRequestWebhookSubscriptionReconfigurationEndpoints
{
    public static IEndpointRouteBuilder MapPaymentRequestWebhookSubscriptionReconfigurationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/v1/webhook-subscriptions")
            .RequireAuthorization()
            .MapPut("/{subscriptionId:guid}", ReconfigureAsync);
        return endpoints;
    }

    private static async Task<IResult> ReconfigureAsync(
        Guid subscriptionId,
        ReconfigurePaymentRequestWebhookSubscriptionRequest request,
        ClaimsPrincipal principal,
        IPaymentRequestWebhookSubscriptionRegistry registry,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryResolveActor(principal, out var actor))
            return Forbidden(httpContext, "Integration or merchant authorization is required.");

        if (subscriptionId == Guid.Empty)
            return Validation(httpContext, "Webhook subscription id cannot be empty.");

        if (request.ExtensionData is { Count: > 0 })
            return Validation(httpContext, "Unsupported request fields. Raw secrets are not accepted.");

        var subscription = await registry.GetAsync(subscriptionId, cancellationToken);
        if (subscription is null || !Owns(actor, subscription))
            return NotFound(httpContext);

        try
        {
            subscription.Reconfigure(
                ParseEndpoint(request.EndpointUrl),
                request.KeyId,
                request.SecretReference,
                request.EventTypes,
                DateTimeOffset.UtcNow);

            await registry.UpdateAsync(subscription, cancellationToken);

            return Results.Ok(new PaymentRequestWebhookSubscriptionReconfigurationResponse(
                subscription.Id,
                subscription.IntegrationId,
                subscription.MerchantId,
                subscription.Endpoint.AbsoluteUri,
                subscription.Status.ToString(),
                subscription.KeyId,
                subscription.SecretReference,
                subscription.EventTypes,
                subscription.UpdatedAtUtc));
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new PaymentRequestWebhookSubscriptionReconfigurationErrorResponse(
                PaymentRequestWebhookSubscriptionReconfigurationErrorCode.Conflict,
                "Webhook subscription reconfiguration conflicts with an existing endpoint.",
                httpContext.TraceIdentifier));
        }
        catch (ArgumentException exception)
        {
            return Validation(httpContext, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new PaymentRequestWebhookSubscriptionReconfigurationErrorResponse(
                PaymentRequestWebhookSubscriptionReconfigurationErrorCode.Conflict,
                exception.Message,
                httpContext.TraceIdentifier));
        }
    }

    private static bool TryResolveActor(ClaimsPrincipal principal, out WebhookSubscriptionActor actor)
    {
        actor = default;
        var integrationRaw = principal.FindFirst("integration_id")?.Value;
        if (string.IsNullOrWhiteSpace(integrationRaw))
            return false;

        string integrationId;
        try
        {
            integrationId = PaymentRequestWebhookSubscription.NormalizeIntegrationId(integrationRaw);
        }
        catch (ArgumentException)
        {
            return false;
        }

        Guid? merchantId = null;
        var merchantRaw = principal.FindFirst("merchant_id")?.Value;
        if (!string.IsNullOrWhiteSpace(merchantRaw))
        {
            if (!Guid.TryParse(merchantRaw, out var parsedMerchantId) || parsedMerchantId == Guid.Empty)
                return false;
            merchantId = parsedMerchantId;
        }

        actor = new WebhookSubscriptionActor(integrationId, merchantId);
        return true;
    }

    private static bool Owns(WebhookSubscriptionActor actor, PaymentRequestWebhookSubscription subscription)
    {
        if (!string.Equals(actor.IntegrationId, subscription.IntegrationId, StringComparison.Ordinal))
            return false;
        return actor.MerchantId is null || subscription.MerchantId == actor.MerchantId;
    }

    private static Uri ParseEndpoint(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value, UriKind.Absolute, out var endpoint))
            throw new ArgumentException("Webhook endpoint must be an absolute HTTP or HTTPS URI.", nameof(value));
        return endpoint;
    }

    private static IResult Forbidden(HttpContext httpContext, string message) =>
        Results.Json(new PaymentRequestWebhookSubscriptionReconfigurationErrorResponse(
            PaymentRequestWebhookSubscriptionReconfigurationErrorCode.Forbidden,
            message,
            httpContext.TraceIdentifier), statusCode: StatusCodes.Status403Forbidden);

    private static IResult Validation(HttpContext httpContext, string message) =>
        Results.BadRequest(new PaymentRequestWebhookSubscriptionReconfigurationErrorResponse(
            PaymentRequestWebhookSubscriptionReconfigurationErrorCode.ValidationError,
            message,
            httpContext.TraceIdentifier));

    private static IResult NotFound(HttpContext httpContext) =>
        Results.NotFound(new PaymentRequestWebhookSubscriptionReconfigurationErrorResponse(
            PaymentRequestWebhookSubscriptionReconfigurationErrorCode.NotFound,
            "Webhook subscription not found.",
            httpContext.TraceIdentifier));

    private readonly record struct WebhookSubscriptionActor(string IntegrationId, Guid? MerchantId);
}
