using System.Security.Claims;
using AfriWallet.Merchants.PaymentIdentity.Application;

namespace IdentityService.Api.MerchantPaymentIdentity;

public static class MerchantPaymentIdentityEndpoints
{
    public static IEndpointRouteBuilder MapMerchantPaymentIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/merchant-payment-identities/{merchantAfWalId}/resolve", ResolveAsync)
            .RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> ResolveAsync(
        string merchantAfWalId,
        ClaimsPrincipal principal,
        IMerchantPaymentIdentityResolver resolver,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var subject = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject))
            return Results.Json(new MerchantPaymentIdentityErrorResponse(
                "merchant_payment_identity_forbidden", "An authenticated subject is required.",
                httpContext.TraceIdentifier), statusCode: StatusCodes.Status403Forbidden);

        try
        {
            var resolution = await resolver.ResolveAsync(merchantAfWalId, $"user:{subject}", cancellationToken);
            return resolution is null
                ? Results.NotFound(new MerchantPaymentIdentityErrorResponse(
                    "merchant_payment_identity_not_found",
                    "Merchant payment identity was not found or is inactive.",
                    httpContext.TraceIdentifier))
                : Results.Ok(new MerchantPaymentIdentityResolutionResponse(
                    resolution.MerchantAfWalId, resolution.MerchantId, resolution.WalletId));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new MerchantPaymentIdentityErrorResponse(
                "merchant_payment_identity_validation_error", exception.Message, httpContext.TraceIdentifier));
        }
    }
}

public sealed record MerchantPaymentIdentityResolutionResponse(string MerchantAfWalId, string MerchantId, Guid WalletId);
public sealed record MerchantPaymentIdentityErrorResponse(string Code, string Message, string TraceId);
