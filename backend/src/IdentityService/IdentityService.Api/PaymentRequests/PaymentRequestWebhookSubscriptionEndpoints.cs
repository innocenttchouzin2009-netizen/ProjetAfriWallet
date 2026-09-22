using System.Security.Claims;
using AfriWallet.PaymentRequests.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.PaymentRequests;

public static class PaymentRequestWebhookSubscriptionEndpoints
{
    public static IEndpointRouteBuilder MapPaymentRequestWebhookSubscriptionManagementEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/webhook-subscriptions")
            .RequireAuthorization();

        group.MapPost("", CreateAsync);
        group.MapGet("", ListAsync);
        group.MapGet("/{subscriptionId:guid}", GetAsync);
        group.MapPut("/{subscriptionId:guid}", UpdateAsync);
        group.MapPost("/{subscriptionId:guid}/disable", DisableAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreatePaymentRequestWebhookSubscriptionRequest request,
        ClaimsPrincipal principal,
        IPaymentRequestWebhookSubscriptionRegistry registry,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryResolveActor(principal, out var actor))
            return Forbidden(httpContext, "Integration or merchant authorization is required.");

        if (HasUnsupportedFields(request.ExtensionData))
            return Validation(httpContext, "Unsupported request fields. Raw secrets are not accepted.");

        try
        {
            var subscription = PaymentRequestWebhookSubscription.Create(
                actor.IntegrationId,
                actor.MerchantId,
                ParseEndpoint(request.EndpointUrl),
                request.KeyId,
                request.SecretReference,
                request.EventTypes,
                DateTimeOffset.UtcNow);

            await registry.AddAsync(subscription, cancellationToken);
            return Results.Created(
                $"/api/v1/webhook-subscriptions/{subscription.Id:D}",
                ToResponse(subscription));
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new PaymentRequestWebhookSubscriptionErrorResponse(
                PaymentRequestWebhookSubscriptionErrorCode.Conflict,
                "A webhook subscription with the same integration and endpoint already exists.",
                httpContext.TraceIdentifier));
        }
        catch (ArgumentException exception)
        {
            return Validation(httpContext, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new PaymentRequestWebhookSubscriptionErrorResponse(
                PaymentRequestWebhookSubscriptionErrorCode.Conflict,
                exception.Message,
                httpContext.TraceIdentifier));
        }
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal principal,
        IPaymentRequestWebhookSubscriptionRegistry registry,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryResolveActor(principal, out var actor))
            return Forbidden(httpContext, "Integration or merchant authorization is required.");

        var subscriptions = await registry.ListForIntegrationAsync(actor.IntegrationId, cancellationToken);
        var visible = subscriptions
            .Where(subscription => actor.MerchantId is null || subscription.MerchantId == actor.MerchantId)
            .Select(ToResponse)
            .ToArray();

        return Results.Ok(visible);
    }

    private static async Task<IResult> GetAsync(
        Guid subscriptionId,
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

        return Results.Ok(ToResponse(subscription));
    }

    private static async Task<IResult> UpdateAsync(
        Guid subscriptionId,
        UpdatePaymentRequestWebhookSubscriptionRequest request,
        ClaimsPrincipal principal,
        IPaymentRequestWebhookSubscriptionRegistry registry,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryResolveActor(principal, out var actor))
            return Forbidden(httpContext, "Integration or merchant authorization is required.");

        if (subscriptionId == Guid.Empty)
            return Validation(httpContext, "Webhook subscription id cannot be empty.");

        if (HasUnsupportedFields(request.ExtensionData))
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
            return Results.Ok(ToResponse(subscription));
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new PaymentRequestWebhookSubscriptionErrorResponse(
                PaymentRequestWebhookSubscriptionErrorCode.Conflict,
                "Webhook subscription update conflicts with an existing endpoint.",
                httpContext.TraceIdentifier));
        }
        catch (ArgumentException exception)
        {
            return Validation(httpContext, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new PaymentRequestWebhookSubscriptionErrorResponse(
                PaymentRequestWebhookSubscriptionErrorCode.Conflict,
                exception.Message,
                httpContext.TraceIdentifier));
        }
    }

    private static async Task<IResult> DisableAsync(
        Guid subscriptionId,
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
            subscription.Disable(DateTimeOffset.UtcNow);
            await registry.UpdateAsync(subscription, cancellationToken);
            return Results.Ok(ToResponse(subscription));
        }
        catch (ArgumentException exception)
        {
            return Validation(httpContext, exception.Message);
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

    private static bool Owns(
        WebhookSubscriptionActor actor,
        PaymentRequestWebhookSubscription subscription)
    {
        if (!string.Equals(actor.IntegrationId, subscription.IntegrationId, StringComparison.Ordinal))
            return false;

        return actor.MerchantId is null || subscription.MerchantId == actor.MerchantId;
    }

    private static bool HasUnsupportedFields(Dictionary<string, System.Text.Json.JsonElement>? fields) =>
        fields is { Count: > 0 };

    private static Uri ParseEndpoint(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value, UriKind.Absolute, out var endpoint))
            throw new ArgumentException("Webhook endpoint must be an absolute HTTP or HTTPS URI.", nameof(value));
        return endpoint;
    }

    private static PaymentRequestWebhookSubscriptionResponse ToResponse(
        PaymentRequestWebhookSubscription subscription) =>
        new(
            subscription.Id,
            subscription.IntegrationId,
            subscription.MerchantId,
            subscription.Endpoint.AbsoluteUri,
            subscription.Status.ToString(),
            subscription.KeyId,
            subscription.SecretReference,
            subscription.EventTypes,
            subscription.CreatedAtUtc,
            subscription.UpdatedAtUtc);

    private static IResult Forbidden(HttpContext httpContext, string message) =>
        Results.Json(
            new PaymentRequestWebhookSubscriptionErrorResponse(
                PaymentRequestWebhookSubscriptionErrorCode.Forbidden,
                message,
                httpContext.TraceIdentifier),
            statusCode: StatusCodes.Status403Forbidden);

    private static IResult Validation(HttpContext httpContext, string message) =>
        Results.BadRequest(new PaymentRequestWebhookSubscriptionErrorResponse(
            PaymentRequestWebhookSubscriptionErrorCode.ValidationError,
            message,
            httpContext.TraceIdentifier));

    private static IResult NotFound(HttpContext httpContext) =>
        Results.NotFound(new PaymentRequestWebhookSubscriptionErrorResponse(
            PaymentRequestWebhookSubscriptionErrorCode.NotFound,
            "Webhook subscription not found.",
            httpContext.TraceIdentifier));

    private readonly record struct WebhookSubscriptionActor(string IntegrationId, Guid? MerchantId);
}
