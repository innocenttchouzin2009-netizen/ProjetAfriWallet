using MobileMoney.Production.Payout.BeneficiaryLookup.Application.Abstractions;
using MobileMoney.Production.Payout.BeneficiaryLookup.Domain;

internal sealed class FakeBeneficiaryAccountHolderResolver(
    Func<string, CameroonMobileOperator, BeneficiaryAccountHolderResolution?> resolve)
    : IBeneficiaryAccountHolderResolver
{
    public int CallCount { get; private set; }

    public Task<BeneficiaryAccountHolderResolution?> ResolveAsync(
        string normalizedPhoneNumber,
        CameroonMobileOperator @operator,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount++;
        return Task.FromResult(resolve(normalizedPhoneNumber, @operator));
    }
}
