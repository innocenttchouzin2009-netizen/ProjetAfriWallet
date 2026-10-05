using MobileMoney.Production.Payout.BeneficiaryLookup.Domain;

namespace MobileMoney.Production.Payout.BeneficiaryLookup.Application.Abstractions;

public interface IBeneficiaryProvider
{
    CameroonMobileOperator Operator { get; }

    Task<BeneficiaryProviderResult> ResolveAsync(
        string normalizedPhoneNumber,
        CancellationToken cancellationToken = default);
}
