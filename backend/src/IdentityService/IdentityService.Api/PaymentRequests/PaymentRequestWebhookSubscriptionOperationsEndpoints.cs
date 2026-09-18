using System.Security.Claims;
using AfriWallet.PaymentRequests.Webhooks;

namespace IdentityService.Api.PaymentRequests;

public static class PaymentRequestWebhookSubscriptionOperationsEndpoints
{
    public static IEndpointRouteBuilder MapPaymentRequestWebhookSubscriptionOperationsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/webhook-subscriptions")
            .RequireAuthorization();

        group.MapPost("/{subscriptionId:guid}/rotate-credentials", RotateCredentialsAsync);
        group.MapPost("/{subscriptionId:guid}/enable", EnableAsync);
        group.MapPost("/{subscriptionId:guid}/test-connectivity", TestConnectivityAsync);
        group.MapGet("/{subscriptionId:guid}/audit", ListAuditAsync);
        group.MapGet("/{subscriptionId:guid}/health", GetHealthAsync);
        return endpoints;
    }

    private static async Task<IResult> RotateCredentialsAsync(
        Guid subscriptionId,
        RotatePaymentRequestWebhookSubscriptionRequest request,
        ClaimsPrincipal principal,
        IPaymentRequestWebhookSubscriptionRegistry registry,
        IPaymentRequestWebhookSigningSecretResolver secretResolver,
        IPaymentRequestWebhookSubscriptionAuditStore auditStore,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryResolveActor(principal, out var actor)) return Forbidden(httpContext);
        if (request.ExtensionData is { Count: > 0 })
            return Validation(httpContext, "Unsupported request fields. Raw secrets are not accepted.");

        var subscription = await OwnedAsync(subscriptionId, actor, registry, cancellationToken);
        if (subscription is null) return NotFound(httpContext);

        try
        {
            _ = await secretResolver.ResolveAsync(request.SecretReference, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            subscription.RotateCredentials(request.KeyId, request.SecretReference, now);
            await registry.UpdateAsync(subscription, cancellationToken);
            await AppendAsync(auditStore, subscription, actor.Subject,
                PaymentRequestWebhookSubscriptionAuditOperation.CredentialsRotated,
                true, null, "Credential references rotated.", now, cancellationToken);
            return Results.Ok(ToResponse(subscription));
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

    private static async Task<IResult> EnableAsync(
        Guid subscriptionId,
        ClaimsPrincipal principal,
        IPaymentRequestWebhookSubscriptionRegistry registry,
        IPaymentRequestWebhookSubscriptionAuditStore auditStore,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryResolveActor(principal, out var actor)) return Forbidden(httpContext);
        var subscription = await OwnedAsync(subscriptionId, actor, registry, cancellationToken);
        if (subscription is null) return NotFound(httpContext);

        try
        {
            var now = DateTimeOffset.UtcNow;
            subscription.Enable(now);
            await registry.UpdateAsync(subscription, cancellationToken);
            await AppendAsync(auditStore, subscription, actor.Subject,
                PaymentRequestWebhookSubscriptionAuditOperation.Enabled,
                true, null, "Subscription enabled.", now, cancellationToken);
            return Results.Ok(ToResponse(subscription));
        }
        catch (ArgumentException exception)
        {
            return Validation(httpContext, exception.Message);
        }
    }

    private static async Task<IResult> TestConnectivityAsync(
        Guid subscriptionId,
        ClaimsPrincipal principal,
        IPaymentRequestWebhookSubscriptionRegistry registry,
        IPaymentRequestWebhookConnectivityProbe probe,
        IPaymentRequestWebhookSubscriptionAuditStore auditStore,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryResolveActor(principal, out var actor)) return Forbidden(httpContext);
        var subscription = await OwnedAsync(subscriptionId, actor, registry, cancellationToken);
        if (subscription is null) return NotFound(httpContext);

        var result = await probe.ProbeAsync(subscription.Endpoint, cancellationToken);
        await AppendAsync(auditStore, subscription, actor.Subject,
            PaymentRequestWebhookSubscriptionAuditOperation.ConnectivityTested,
            result.Succeeded, result.HttpStatusCode, result.Detail, result.ObservedAtUtc, cancellationToken);

        return Results.Ok(new PaymentRequestWebhookConnectivityTestResponse(
            subscription.Id,
            result.Succeeded,
            result.HttpStatusCode,
            result.Detail,
            result.ObservedAtUtc));
    }

    private static async Task<IResult> ListAuditAsync(
        Guid subscriptionId,
        ClaimsPrincipal principal,
        IPaymentRequestWebhookSubscriptionRegistry registry,
        IPaymentRequestWebhookSubscriptionAuditStore auditStore,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryResolveActor(principal, out var actor)) return Forbidden(httpContext);
        var subscription = await OwnedAsync(subscriptionId, actor, registry, cancellationToken);
        if (subscription is null) return NotFound(httpContext);

        var audit = await auditStore.ListAsync(subscription.Id, cancellationToken);
        return Results.Ok(audit.Select(x => new PaymentRequestWebhookSubscriptionAuditResponse(
            x.Id,
            x.Operation.ToString(),
            x.ActorSubject,
            x.KeyId,
            x.SecretReference,
            x.Succeeded,
            x.HttpStatusCode,
            x.Detail,
            x.OccurredAtUtc)).ToArray());
    }

    private static async Task<IResult> GetHealthAsync(
        Guid subscriptionId,
        ClaimsPrincipal principal,
        IPaymentRequestWebhookSubscriptionRegistry registry,
        IPaymentRequestWebhookSubscriptionAuditStore auditStore,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryResolveActor(principal, out var actor)) return Forbidden(httpContext);
        var subscription = await OwnedAsync(subscriptionId, actor, registry, cancellationToken);
        if (subscription is null) return NotFound(httpContext);

        var audit = await auditStore.ListAsync(subscription.Id, cancellationToken);
        var lastProbe = audit.FirstOrDefault(x =>
            x.Operation == PaymentRequestWebhookSubscriptionAuditOperation.ConnectivityTested);

        var health = subscription.Status == PaymentRequestWebhookSubscriptionStatus.Disabled
            ? "Disabled"
            : lastProbe is null
                ? "Unknown"
                : lastProbe.Succeeded ? "Healthy" : "Degraded";

        return Results.Ok(new PaymentRequestWebhookDeliveryHealthResponse(
            subscription.Id,
            subscription.Status.ToString(),
            lastProbe?.OccurredAtUtc,
            lastProbe?.Succeeded,
            lastProbe?.HttpStatusCode,
            health));
    }

    private static async Task<PaymentRequestWebhookSubscription?> OwnedAsync(
        Guid subscriptionId,
        WebhookActor actor,
        IPaymentRequestWebhookSubscriptionRegistry registry,
        CancellationToken cancellationToken)
    {
        if (subscriptionId == Guid.Empty) return null;
        var subscription = await registry.GetAsync(subscriptionId, cancellationToken);
        if (subscription is null) return null;
        if (!string.Equals(subscription.IntegrationId, actor.IntegrationId, StringComparison.Ordinal)) return null;
        if (actor.MerchantId is not null && subscription.MerchantId != actor.MerchantId) return null;
        return subscription;
    }

    private static bool TryResolveActor(ClaimsPrincipal principal, out WebhookActor actor)
    {
        actor = default;
        var subject = principal.FindFirst("sub")?.Value;
        var integrationRaw = principal.FindFirst("integration_id")?.Value;
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(integrationRaw)) return false;

        string integrationId;
        try { integrationId = PaymentRequestWebhookSubscription.NormalizeIntegrationId(integrationRaw); }
        catch (ArgumentException) { return false; }

        Guid? merchantId = null;
        var merchantRaw = principal.FindFirst("merchant_id")?.Value;
        if (!string.IsNullOrWhiteSpace(merchantRaw))
        {
            if (!Guid.TryParse(merchantRaw, out var parsed) || parsed == Guid.Empty) return false;
            merchantId = parsed;
        }

        actor = new WebhookActor(subject, integrationId, merchantId);
        return true;
    }

    private static Task AppendAsync(
        IPaymentRequestWebhookSubscriptionAuditStore store,
        PaymentRequestWebhookSubscription subscription,
        string actorSubject,
        PaymentRequestWebhookSubscriptionAuditOperation operation,
        bool succeeded,
        int? httpStatusCode,
        string detail,
        DateTimeOffset atUtc,
        CancellationToken cancellationToken) =>
        store.AppendAsync(new PaymentRequestWebhookSubscriptionAuditEntry(
            Guid.NewGuid(),
            subscription.Id,
            subscription.IntegrationId,
            subscription.MerchantId,
            actorSubject,
            operation,
            subscription.KeyId,
            subscription.SecretReference,
            succeeded,
            httpStatusCode,
            detail,
            atUtc), cancellationToken);

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

    private static IResult Forbidden(HttpContext httpContext) =>
        Results.Json(new PaymentRequestWebhookSubscriptionErrorResponse(
            PaymentRequestWebhookSubscriptionErrorCode.Forbidden,
            "Integration or merchant authorization is required.",
            httpContext.TraceIdentifier), statusCode: StatusCodes.Status403Forbidden);

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

    private readonly record struct WebhookActor(string Subject, string IntegrationId, Guid? MerchantId);
}
