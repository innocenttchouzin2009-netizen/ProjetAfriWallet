using MobileMoney.Production.Payout.Abstractions;
using MobileMoney.Production.Payout.Contracts;
using MobileMoney.Production.Payout.Domain;

namespace MobileMoney.Production.Payout.Extensions;

public static class MobileMoneyPayoutEndpointExtensions
{
    public static IEndpointRouteBuilder MapMobileMoneyPayoutEligibility(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost(
            "/api/v1/mobile-money/payouts/eligibility",
            async (
                MobileMoneyPayoutEligibilityRequest request,
                IMobileMoneyPayoutEligibilityPolicy policy,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var corridor = new MobileMoneyPayoutCorridor(
                        request.SourceCountryCode,
                        request.SourceCurrency,
                        request.DestinationCountryCode,
                        request.DestinationCurrency,
                        request.OperatorCode);

                    var result = await policy.EvaluateAsync(
                        corridor,
                        cancellationToken);

                    return Results.Ok(
                        new MobileMoneyPayoutEligibilityResponse(
                            result.IsEligible,
                            result.FailureCode));
                }
                catch (ArgumentException)
                {
                    return Results.BadRequest(
                        new MobileMoneyPayoutErrorResponse(
                            "PAYOUT_INVALID_DESTINATION",
                            "The payout destination or corridor is invalid.",
                            null));
                }
            });

        return endpoints;
    }
}
