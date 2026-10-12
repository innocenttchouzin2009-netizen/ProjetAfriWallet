using MobileMoney.Production.Authentication;
using MobileMoney.Production.Correlation;
using MobileMoney.Production.Payout.Contracts;
using MobileMoney.Production.Payout.Execution.Application;
using MobileMoney.Production.Payout.Execution.Contracts;

namespace MobileMoney.Production.Payout.Execution.Extensions;

public static class MobileMoneyPayoutExecutionEndpointExtensions
{
    public const string Route = "/api/v1/mobile-money/payouts/execution/prepare";
    public const string IdempotencyHeaderName = "Idempotency-Key";

    public static IEndpointRouteBuilder MapMobileMoneyPayoutExecution(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost(Route, HandleAsync)
            .RequireAfWalSession();

        return endpoints;
    }

    public static async Task<IResult> HandleAsync(
        PrepareMobileMoneyPayoutExecutionHttpRequest request,
        MobileMoneyPayoutExecutionOrchestrator orchestrator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(orchestrator);
        ArgumentNullException.ThrowIfNull(httpContext);

        var idempotencyValues =
            httpContext.Request.Headers[IdempotencyHeaderName];

        if (idempotencyValues.Count != 1
            || string.IsNullOrWhiteSpace(idempotencyValues[0]))
        {
            return Error(
                httpContext,
                StatusCodes.Status400BadRequest,
                MobileMoneyPayoutExecutionErrorCodes.IdempotencyRequired,
                "A single non-empty Idempotency-Key header is required.");
        }

        try
        {
            var response = await orchestrator.PrepareAsync(
                new PrepareMobileMoneyPayoutExecutionRequest(
                    request.QuoteId,
                    request.FundingCorrelationId,
                    idempotencyValues[0]!.Trim()),
                cancellationToken);

            return TypedResults.Ok(response);
        }
        catch (MobileMoneyPayoutExecutionPreparationException exception)
        {
            var statusCode = exception.Code switch
            {
                MobileMoneyPayoutExecutionErrorCodes.QuoteNotFound =>
                    StatusCodes.Status404NotFound,
                MobileMoneyPayoutExecutionErrorCodes.FundingNotFound =>
                    StatusCodes.Status404NotFound,
                MobileMoneyPayoutExecutionErrorCodes.IdempotencyConflict =>
                    StatusCodes.Status409Conflict,
                MobileMoneyPayoutExecutionErrorCodes.BindingInvalid =>
                    StatusCodes.Status422UnprocessableEntity,
                _ => StatusCodes.Status422UnprocessableEntity
            };

            return Error(
                httpContext,
                statusCode,
                exception.Code,
                "The payout execution could not be prepared.");
        }
        catch (Exception exception)
            when (exception is ArgumentException or OverflowException)
        {
            return Error(
                httpContext,
                StatusCodes.Status400BadRequest,
                MobileMoneyPayoutExecutionErrorCodes.InvalidRequest,
                "The payout execution request is invalid.");
        }
    }

    private static IResult Error(
        HttpContext httpContext,
        int statusCode,
        string code,
        string message)
    {
        var correlationId =
            CorrelationContext.FromHttpContext(httpContext)?.CorrelationId
            ?? httpContext.TraceIdentifier;

        return TypedResults.Json(
            new MobileMoneyPayoutErrorResponse(
                code,
                message,
                correlationId),
            statusCode: statusCode);
    }
}
