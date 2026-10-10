using MobileMoney.Production.Payout.Funding.Application;
using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Funding.Abstractions;

public interface IMobileMoneyPayoutFundingRecoveryProbe
{
    bool Supports(FundingSourceType sourceType);

    Task<FundingRecoveryProviderResult> RecoverAsync(
        FundingRecoveryProviderRequest request,
        CancellationToken cancellationToken = default);
}
