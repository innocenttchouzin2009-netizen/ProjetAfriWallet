using System.Security.Claims;
using AfriWallet.PaymentRequests.Webhooks;

namespace IdentityService.Api.PaymentRequests;

public static class PaymentRequestWebhookSubscriptionActivationEndpoints
{
    public static IEndpointRouteBuilder MapPaymentRequestWebhookSubscriptionActivationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/webhook-subscriptions")
            .RequireAuthorization();

        group.MapPost("/{subscriptionId:guid}/disable", DisableAsync);
        group.MapPost("/{subscriptionId:guid}/enable", EnableAsync);
        return endpoints;
    }

    private static Task<IResult> DisableAsync(
        Guid subscriptionId,
        ClaimsPrincipal principal,
        IPaymentRequestWebhookSubscriptionRegistry registry,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(subscriptionId, enable: false, principal, registry, httpContext, cancellationToken);

    private static Task<IResult> EnableAsync(
        Guid subscriptionId,
        ClaimsPrincipal principal,
        IPaymentRequestWebhookSubscriptionRegistry registry,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(subscriptionId, enable: true, principal, registry, httpContext, cancellationToken);

    private static async Task<IResult> ChangeStatusAsync(
        Guid subscriptionId,
        bool enable,
        ClaimsPrincipal principal,
        IPaymentRequestWebhookSubscriptionRegistry registry,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryResolveActor(principal, out var actor))
            return Forbidden(httpContext, "Integration or merchant authorization is required.");

        if (subscriptionId == Guid.Empty)
            return Validation(httpContext, "Webhook subscription id cannot be empty.");

        var subscription = await registry.GetAsync(subscriptionId, cancellationToken);
        if (subscription is null || !Owns(actor, subscription))
            return NotFound(httpContext);

        try
        {
            var atUtc = DateTimeOffset.UtcNow;
            if (enable)
                subscription.Enable(atUtc);
            else
                subscription.Disable(atUtc);

            await registry.UpdateAsync(subscription, cancellationToken);

            return Results.Ok(new PaymentRequestWebhookSubscriptionActivationResponse(
                subscription.Id,
                subscription.IntegrationId,
                subscription.MerchantId,
                subscription.Endpoint.AbsoluteUri,
                subscription.Status.ToString(),
                subscription.KeyId,
                subscription.SecretReference,
                subscription.EventTypes,
                subscription.CreatedAtUtc,
                subscription.UpdatedAtUtc));
        }
        catch (ArgumentException exception)
        {
            return Validation(httpContext, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new PaymentRequestWebhookSubscriptionActivationErrorResponse(
                PaymentRequestWebhookSubscriptionActivationErrorCode.Conflict,
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

    private static IResult Forbidden(HttpContext httpContext, string message) =>
        Results.Json(new PaymentRequestWebhookSubscriptionActivationErrorResponse(
            PaymentRequestWebhookSubscriptionActivationErrorCode.Forbidden,
            message,
            httpContext.TraceIdentifier), statusCode: StatusCodes.Status403Forbidden);

    private static IResult Validation(HttpContext httpContext, string message) =>
        Results.BadRequest(new PaymentRequestWebhookSubscriptionActivationErrorResponse(
            PaymentRequestWebhookSubscriptionActivationErrorCode.ValidationError,
            message,
            httpContext.TraceIdentifier));

    private static IResult NotFound(HttpContext httpContext) =>
        Results.NotFound(new PaymentRequestWebhookSubscriptionActivationErrorResponse(
            PaymentRequestWebhookSubscriptionActivationErrorCode.NotFound,
            "Webhook subscription not found.",
            httpContext.TraceIdentifier));

    private readonly record struct WebhookSubscriptionActor(string IntegrationId, Guid? MerchantId);
}
