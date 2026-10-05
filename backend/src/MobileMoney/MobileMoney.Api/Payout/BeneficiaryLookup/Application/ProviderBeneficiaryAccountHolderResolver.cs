using MobileMoney.Production.Payout.BeneficiaryLookup.Application.Abstractions;
using MobileMoney.Production.Payout.BeneficiaryLookup.Domain;

namespace MobileMoney.Production.Payout.BeneficiaryLookup.Application;

public sealed class ProviderBeneficiaryAccountHolderResolver(
    IEnumerable<IBeneficiaryProvider> providers)
    : IBeneficiaryAccountHolderResolver
{
    private readonly IReadOnlyDictionary<CameroonMobileOperator, IBeneficiaryProvider> _providers =
        providers.ToDictionary(provider => provider.Operator);

    public async Task<BeneficiaryAccountHolderResolution?> ResolveAsync(
        string normalizedPhoneNumber,
        CameroonMobileOperator @operator,
        CancellationToken cancellationToken = default)
    {
        if (!_providers.TryGetValue(@operator, out var provider))
            return null;

        var result = await provider.ResolveAsync(normalizedPhoneNumber, cancellationToken);

        if (result.Status != BeneficiaryProviderStatus.Resolved ||
            string.IsNullOrWhiteSpace(result.AccountHolderName))
        {
            return null;
        }

        return new BeneficiaryAccountHolderResolution(result.AccountHolderName);
    }
}
