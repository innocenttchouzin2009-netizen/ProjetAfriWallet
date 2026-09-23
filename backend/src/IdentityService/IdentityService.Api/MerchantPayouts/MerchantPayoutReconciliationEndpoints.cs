using System.Security.Claims;
using AfriWallet.Merchants.Payout.Application;
using AfriWallet.Merchants.Payout.Domain;

namespace IdentityService.Api.MerchantPayouts;

public static class MerchantPayoutReconciliationEndpoints
{
    public static IEndpointRouteBuilder MapMerchantPayoutReconciliationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/merchant-payouts")
            .RequireAuthorization();

        group.MapPost("/{payoutId:guid}/reconciliation/provider-results", RecordAndReconcileAsync);
        group.MapGet("/{payoutId:guid}/reconciliation/latest", GetLatestAsync);

        return endpoints;
    }

    private static async Task<IResult> RecordAndReconcileAsync(
        Guid payoutId,
        ReconcileMerchantPayoutProviderResultRequest request,
        ClaimsPrincipal principal,
        IMerchantPayoutRepository payouts,
        MerchantPayoutReconciliationService reconciliation,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryResolveMerchant(principal, out var merchantId))
            return Forbidden(httpContext);

        if (payoutId == Guid.Empty || request.ResultId == Guid.Empty)
            return Validation(httpContext, "Payout id and provider result id are required.");

        var payout = await payouts.GetAsync(payoutId, cancellationToken);
        if (payout is null || !string.Equals(payout.MerchantId, merchantId, StringComparison.Ordinal))
            return NotFound(httpContext);

        try
        {
            var result = await reconciliation.RecordAndReconcileAsync(
                new ReconcileMerchantPayoutProviderResultCommand(
                    request.ResultId,
                    payoutId,
                    request.Status,
                    request.ProviderReference,
                    request.FailureCode,
                    request.AmountMinor,
                    request.Currency,
                    request.ObservedAtUtc),
                cancellationToken);

            return Results.Ok(ToResponse(result));
        }
        catch (ArgumentException exception)
        {
            return Validation(httpContext, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new MerchantPayoutReconciliationErrorResponse(
                MerchantPayoutReconciliationErrorCode.Conflict,
                exception.Message,
                httpContext.TraceIdentifier));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(httpContext);
        }
    }

    private static async Task<IResult> GetLatestAsync(
        Guid payoutId,
        ClaimsPrincipal principal,
        IMerchantPayoutRepository payouts,
        MerchantPayoutReconciliationService reconciliation,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryResolveMerchant(principal, out var merchantId))
            return Forbidden(httpContext);

        if (payoutId == Guid.Empty)
            return Validation(httpContext, "Payout id is required.");

        var payout = await payouts.GetAsync(payoutId, cancellationToken);
        if (payout is null || !string.Equals(payout.MerchantId, merchantId, StringComparison.Ordinal))
            return NotFound(httpContext);

        var latest = await reconciliation.GetLatestAsync(payoutId, cancellationToken);
        return latest is null
            ? NotFound(httpContext)
            : Results.Ok(ToResponse(latest));
    }

    private static bool TryResolveMerchant(ClaimsPrincipal principal, out string merchantId)
    {
        merchantId = principal.FindFirst("merchant_id")?.Value?.Trim() ?? string.Empty;
        return merchantId.Length is > 0 and <= 128;
    }

    private static MerchantPayoutReconciliationResponse ToResponse(
        MerchantPayoutReconciliationResult result) =>
        new(
            result.ReconciliationId,
            result.PayoutId,
            result.ResultId,
            result.MerchantId,
            result.Status.ToString(),
            result.ReasonCode,
            result.EvaluatedAtUtc);

    private static IResult Forbidden(HttpContext httpContext) =>
        Results.Json(
            new MerchantPayoutReconciliationErrorResponse(
                MerchantPayoutReconciliationErrorCode.Forbidden,
                "Merchant authorization is required.",
                httpContext.TraceIdentifier),
            statusCode: StatusCodes.Status403Forbidden);

    private static IResult Validation(HttpContext httpContext, string message) =>
        Results.BadRequest(new MerchantPayoutReconciliationErrorResponse(
            MerchantPayoutReconciliationErrorCode.ValidationError,
            message,
            httpContext.TraceIdentifier));

    private static IResult NotFound(HttpContext httpContext) =>
        Results.NotFound(new MerchantPayoutReconciliationErrorResponse(
            MerchantPayoutReconciliationErrorCode.NotFound,
            "Merchant payout reconciliation was not found.",
            httpContext.TraceIdentifier));
}
