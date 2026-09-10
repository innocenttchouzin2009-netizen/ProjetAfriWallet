using AfriWallet.Fx.Application;

namespace IdentityService.Api.Fx;

public static class FxEndpoints
{
    public static IEndpointRouteBuilder MapFxEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/fx")
            .RequireAuthorization();

        group.MapGet("/quotes/{sourceCurrencyCode}/{targetCurrencyCode}", GetQuoteAsync);

        return endpoints;
    }

    private static async Task<IResult> GetQuoteAsync(
        string sourceCurrencyCode,
        string targetCurrencyCode,
        FxQuoteApplicationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await service.GetQuoteAsync(
            new FxQuoteRequest(sourceCurrencyCode, targetCurrencyCode),
            cancellationToken);

        if (result.Succeeded && result.Value is not null)
        {
            return Results.Ok(new FxQuoteResponse(
                result.Value.SourceCurrencyCode,
                result.Value.TargetCurrencyCode,
                result.Value.Rate,
                result.Value.QuotedAtUtc));
        }

        var error = new FxErrorResponse(
            result.ErrorCode ?? FxQuoteErrorCode.QuoteUnavailable,
            result.ErrorMessage ?? "FX quote is unavailable.",
            httpContext.TraceIdentifier);

        return result.ErrorCode switch
        {
            FxQuoteErrorCode.ValidationError => Results.BadRequest(error),
            FxQuoteErrorCode.QuoteUnavailable => Results.NotFound(error),
            FxQuoteErrorCode.ProviderQuoteMismatch => Results.Json(
                error,
                statusCode: StatusCodes.Status502BadGateway),
            _ => Results.Json(error, statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
