using System.Security.Claims;
using AfriWallet.Webhooks.Application;
using AfriWallet.Webhooks.Domain;

namespace IdentityService.Api.Webhooks;

public static class WebhookManagementEndpoints
{
    public static IEndpointRouteBuilder MapWebhookManagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/webhook-subscriptions")
            .RequireAuthorization();

        group.MapPost("", RegisterAsync);
        group.MapGet("", ListAsync);
        group.MapGet("/{subscriptionId:guid}", GetAsync);
        group.MapPost("/{subscriptionId:guid}/disable", DisableAsync);
        group.MapPost("/{subscriptionId:guid}/enable", EnableAsync);

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterWebhookSubscriptionRequest request,
        ClaimsPrincipal principal,
        WebhookManagementService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(principal, out var ownerId))
            return Unauthorized(httpContext);

        if (request.ExtensionData is { Count: > 0 })
            return Validation(httpContext, "Unsupported request fields. Owner identity is derived from the authenticated token.");

        if (string.IsNullOrWhiteSpace(request.Endpoint) ||
            request.EventTypes is null ||
            string.IsNullOrWhiteSpace(request.SigningKeyReference))
        {
            return Validation(httpContext, "Endpoint, event types and signing key reference are required.");
        }

        if (!Uri.TryCreate(request.Endpoint, UriKind.Absolute, out var endpoint))
            return Validation(httpContext, "Webhook endpoint must be an absolute HTTPS URI.");

        try
        {
            var created = await service.RegisterAsync(
                new RegisterWebhookSubscriptionCommand(
                    ownerId,
                    endpoint,
                    request.EventTypes,
                    request.SigningKeyReference,
                    DateTimeOffset.UtcNow),
                cancellationToken);

            return Results.Created(
                $"/api/v1/webhook-subscriptions/{created.Id:D}",
                ToResponse(created));
        }
        catch (ArgumentException exception)
        {
            return Validation(httpContext, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(httpContext, exception.Message);
        }
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal principal,
        WebhookManagementService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(principal, out var ownerId))
            return Unauthorized(httpContext);

        var subscriptions = await service.ListAsync(ownerId, cancellationToken);
        return Results.Ok(subscriptions.Select(ToResponse).ToArray());
    }

    private static async Task<IResult> GetAsync(
        Guid subscriptionId,
        ClaimsPrincipal principal,
        WebhookManagementService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(principal, out var ownerId))
            return Unauthorized(httpContext);

        try
        {
            var subscription = await service.GetOwnedAsync(
                ownerId,
                WebhookSubscriptionId.From(subscriptionId),
                cancellationToken);

            return subscription is null
                ? NotFound(httpContext)
                : Results.Ok(ToResponse(subscription));
        }
        catch (ArgumentException exception)
        {
            return Validation(httpContext, exception.Message);
        }
    }

    private static Task<IResult> DisableAsync(
        Guid subscriptionId,
        ClaimsPrincipal principal,
        WebhookManagementService service,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(subscriptionId, principal, service, httpContext, enable: false, cancellationToken);

    private static Task<IResult> EnableAsync(
        Guid subscriptionId,
        ClaimsPrincipal principal,
        WebhookManagementService service,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(subscriptionId, principal, service, httpContext, enable: true, cancellationToken);

    private static async Task<IResult> ChangeStatusAsync(
        Guid subscriptionId,
        ClaimsPrincipal principal,
        WebhookManagementService service,
        HttpContext httpContext,
        bool enable,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(principal, out var ownerId))
            return Unauthorized(httpContext);

        try
        {
            var id = WebhookSubscriptionId.From(subscriptionId);
            var changedAtUtc = DateTimeOffset.UtcNow;
            var subscription = enable
                ? await service.EnableAsync(ownerId, id, changedAtUtc, cancellationToken)
                : await service.DisableAsync(ownerId, id, changedAtUtc, cancellationToken);

            return subscription is null
                ? NotFound(httpContext)
                : Results.Ok(ToResponse(subscription));
        }
        catch (ArgumentException exception)
        {
            return Validation(httpContext, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(httpContext, exception.Message);
        }
    }

    private static WebhookSubscriptionResponse ToResponse(WebhookSubscriptionSnapshot subscription) =>
        new(
            subscription.Id,
            subscription.Endpoint,
            subscription.EventTypes,
            subscription.SigningKeyReference,
            subscription.Status,
            subscription.CreatedAtUtc,
            subscription.UpdatedAtUtc);

    private static bool TryGetOwnerId(ClaimsPrincipal principal, out Guid ownerId) =>
        Guid.TryParse(principal.FindFirst("sub")?.Value, out ownerId) && ownerId != Guid.Empty;

    private static IResult Unauthorized(HttpContext httpContext) =>
        Results.Json(
            new WebhookManagementErrorResponse(
                WebhookManagementErrorCode.Unauthorized,
                "Authenticated user id is missing.",
                httpContext.TraceIdentifier),
            statusCode: StatusCodes.Status401Unauthorized);

    private static IResult Validation(HttpContext httpContext, string message) =>
        Results.BadRequest(new WebhookManagementErrorResponse(
            WebhookManagementErrorCode.ValidationError,
            message,
            httpContext.TraceIdentifier));

    private static IResult NotFound(HttpContext httpContext) =>
        Results.NotFound(new WebhookManagementErrorResponse(
            WebhookManagementErrorCode.NotFound,
            "Webhook subscription not found.",
            httpContext.TraceIdentifier));

    private static IResult Conflict(HttpContext httpContext, string message) =>
        Results.Conflict(new WebhookManagementErrorResponse(
            WebhookManagementErrorCode.Conflict,
            message,
            httpContext.TraceIdentifier));
}
