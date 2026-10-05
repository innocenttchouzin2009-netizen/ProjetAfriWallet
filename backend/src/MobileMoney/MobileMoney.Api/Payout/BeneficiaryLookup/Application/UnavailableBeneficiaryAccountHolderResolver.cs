using MobileMoney.Production.Payout.BeneficiaryLookup.Application.Abstractions;
using MobileMoney.Production.Payout.BeneficiaryLookup.Domain;

namespace MobileMoney.Production.Payout.BeneficiaryLookup.Application;

public sealed class UnavailableBeneficiaryAccountHolderResolver
    : IBeneficiaryAccountHolderResolver
{
    public Task<BeneficiaryAccountHolderResolution?> ResolveAsync(
        string normalizedPhoneNumber,
        CameroonMobileOperator @operator,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<BeneficiaryAccountHolderResolution?>(null);
    }
}
