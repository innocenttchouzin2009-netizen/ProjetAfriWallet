using MobileMoney.Production.Payout.BeneficiaryLookup.Application;
using MobileMoney.Production.Payout.BeneficiaryLookup.Contracts;
using MobileMoney.Production.Payout.BeneficiaryLookup.Domain;

namespace MobileMoney.Production.Payout.Extensions;

public static class MobileMoneyBeneficiaryLookupEndpointExtensions
{
    public const string Route = "/api/v1/mobile-money/beneficiaries/lookup";

    public static IEndpointRouteBuilder MapMobileMoneyBeneficiaryLookup(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost(
            Route,
            async (
                BeneficiaryLookupRequest request,
                CameroonOperatorResolver operatorResolver,
                BeneficiaryAccountHolderLookupService holderLookupService,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var operatorResolution = operatorResolver.Resolve(request.PhoneNumber);

                    if (!operatorResolution.IsResolved)
                    {
                        return Results.Ok(
                            new BeneficiaryLookupHttpResponse(
                                operatorResolution.NormalizedPhoneNumber,
                                "CM",
                                null,
                                OperatorResolved: false,
                                AccountHolderName: null,
                                BeneficiaryResolved: false));
                    }

                    var holder = await holderLookupService.LookupAsync(
                        new BeneficiaryAccountHolderLookupRequest(
                            operatorResolution.NormalizedPhoneNumber,
                            operatorResolution.Operator!.Value),
                        cancellationToken);

                    return Results.Ok(
                        new BeneficiaryLookupHttpResponse(
                            holder.NormalizedPhoneNumber,
                            holder.CountryCode,
                            ToApiOperator(holder.Operator),
                            OperatorResolved: true,
                            holder.AccountHolderName,
                            holder.BeneficiaryResolved));
                }
                catch (ArgumentException)
                {
                    return Results.BadRequest(
                        new BeneficiaryLookupErrorResponse(
                            "BENEFICIARY_LOOKUP_INVALID_PHONE",
                            "The beneficiary phone number is invalid or unsupported."));
                }
            });

        return endpoints;
    }

    private static string ToApiOperator(CameroonMobileOperator @operator) =>
        @operator switch
        {
            CameroonMobileOperator.Mtn => "MTN",
            CameroonMobileOperator.Orange => "ORANGE",
            _ => throw new ArgumentOutOfRangeException(nameof(@operator))
        };
}
