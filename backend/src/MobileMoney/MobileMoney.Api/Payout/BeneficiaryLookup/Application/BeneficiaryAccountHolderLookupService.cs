using MobileMoney.Production.Payout.BeneficiaryLookup.Application.Abstractions;
using MobileMoney.Production.Payout.BeneficiaryLookup.Contracts;

namespace MobileMoney.Production.Payout.BeneficiaryLookup.Application;

public sealed class BeneficiaryAccountHolderLookupService(
    IBeneficiaryAccountHolderResolver accountHolderResolver)
{
    private const string CountryCode = "CM";

    public async Task<BeneficiaryAccountHolderLookupResponse> LookupAsync(
        BeneficiaryAccountHolderLookupRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureAlreadyNormalizedCameroonNumber(request.NormalizedPhoneNumber);

        var resolution = await accountHolderResolver.ResolveAsync(
            request.NormalizedPhoneNumber,
            request.Operator,
            cancellationToken);

        var accountHolderName = resolution?.AccountHolderName.Trim();
        if (string.IsNullOrWhiteSpace(accountHolderName))
            accountHolderName = null;

        return new BeneficiaryAccountHolderLookupResponse(
            request.NormalizedPhoneNumber,
            CountryCode,
            request.Operator,
            accountHolderName,
            BeneficiaryResolved: accountHolderName is not null);
    }

    private static void EnsureAlreadyNormalizedCameroonNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length != 13 ||
            !value.StartsWith("+2376", StringComparison.Ordinal) ||
            value[1..].Any(ch => ch is < '0' or > '9'))
        {
            throw new ArgumentException(
                "Beneficiary lookup requires an already normalized Cameroon E.164 mobile number.",
                nameof(value));
        }
    }
}
