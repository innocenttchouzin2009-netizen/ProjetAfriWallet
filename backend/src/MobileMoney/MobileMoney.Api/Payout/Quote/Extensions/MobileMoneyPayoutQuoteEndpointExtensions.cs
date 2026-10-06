using MobileMoney.Production.Correlation;
using MobileMoney.Production.Payout.Application;
using MobileMoney.Production.Payout.Contracts;
using MobileMoney.Production.Payout.Quote.Application;
using MobileMoney.Production.Payout.Quote.Contracts;

namespace MobileMoney.Production.Payout.Quote.Extensions;

public static class MobileMoneyPayoutQuoteEndpointExtensions
{
    public const string Route = "/api/v1/mobile-money/payouts/quote";

    public static IEndpointRouteBuilder MapMobileMoneyPayoutQuote(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost(Route, HandleAsync);

        return endpoints;
    }

    public static async Task<IResult> HandleAsync(
        CreateMobileMoneyPayoutQuoteRequest request,
        MobileMoneyPayoutQuoteOrchestrator orchestrator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(orchestrator);
        ArgumentNullException.ThrowIfNull(httpContext);

        try
        {
            var response = await orchestrator.CreateAsync(
                request,
                cancellationToken);

            return TypedResults.Ok(response);
        }
        catch (MobileMoneyPayoutEligibilityException exception)
        {
            return Error(
                httpContext,
                StatusCodes.Status422UnprocessableEntity,
                exception.Code,
                "The requested payout corridor is not eligible.");
        }
        catch (MobileMoneyPayoutQuoteUnavailableException exception)
        {
            return Error(
                httpContext,
                StatusCodes.Status503ServiceUnavailable,
                exception.Code,
                "A payout quote is temporarily unavailable.");
        }
        catch (KeyNotFoundException)
        {
            return Error(
                httpContext,
                StatusCodes.Status503ServiceUnavailable,
                "PAYOUT_QUOTE_PRICING_UNAVAILABLE",
                "Payout pricing is not configured for the requested corridor.");
        }
        catch (Exception exception)
            when (exception is ArgumentException or OverflowException)
        {
            return Error(
                httpContext,
                StatusCodes.Status400BadRequest,
                "PAYOUT_QUOTE_INVALID_REQUEST",
                "The payout quote request is invalid.");
        }
    }

    private static IResult Error(
        HttpContext httpContext,
        int statusCode,
        string code,
        string message)
    {
        var correlationId =
            CorrelationContext.FromHttpContext(httpContext)?.CorrelationId;

        return TypedResults.Json(
            new MobileMoneyPayoutErrorResponse(
                code,
                message,
                correlationId),
            statusCode: statusCode);
    }
}
