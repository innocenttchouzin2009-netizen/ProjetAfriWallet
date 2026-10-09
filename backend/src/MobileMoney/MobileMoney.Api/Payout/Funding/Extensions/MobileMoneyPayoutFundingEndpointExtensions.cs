using MobileMoney.Production.Correlation;
using MobileMoney.Production.Payout.Contracts;
using MobileMoney.Production.Payout.Funding.Application;
using MobileMoney.Production.Payout.Funding.Contracts;
using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Funding.Extensions;

public static class MobileMoneyPayoutFundingEndpointExtensions
{
    public const string Route = "/api/v1/mobile-money/payouts/funding/plan";

    public static IEndpointRouteBuilder MapMobileMoneyPayoutFunding(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost(Route, HandleAsync);

        return endpoints;
    }

    public static async Task<IResult> HandleAsync(
        PlanMobileMoneyPayoutFundingRequest request,
        MobileMoneyPayoutFundingPlanningService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(httpContext);

        try
        {
            var allocations = request.Allocations
                .Select(allocation => FundingAllocation.Create(
                    allocation.SourceId,
                    allocation.SourceType,
                    allocation.AmountMinor,
                    allocation.CurrencyCode))
                .ToArray();

            var plan = await service.PlanAsync(
                new PlanMobileMoneyPayoutFundingCommand(
                    request.CorrelationId,
                    request.RequiredAmountMinor,
                    request.CurrencyCode,
                    allocations,
                    request.RequestedAtUtc),
                cancellationToken);

            return TypedResults.Ok(
                new MobileMoneyPayoutFundingPlanResponse(
                    plan.CorrelationId,
                    plan.RequiredAmountMinor,
                    plan.CurrencyCode,
                    plan.Allocations
                        .Select(allocation =>
                            new MobileMoneyPayoutFundingAllocationResponse(
                                allocation.SourceId,
                                allocation.SourceType,
                                allocation.AmountMinor,
                                allocation.CurrencyCode))
                        .ToArray(),
                    plan.PlannedAtUtc));
        }
        catch (Exception exception)
            when (exception is ArgumentException or OverflowException)
        {
            return Error(
                httpContext,
                StatusCodes.Status400BadRequest,
                "PAYOUT_FUNDING_INVALID_REQUEST",
                "The payout funding request is invalid.");
        }
        catch (InvalidOperationException)
        {
            return Error(
                httpContext,
                StatusCodes.Status422UnprocessableEntity,
                "PAYOUT_FUNDING_REJECTED",
                "The payout funding plan could not be accepted.");
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
