using MobileMoney.Production.Payout.Funding.Application;
using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Funding.Abstractions;

public interface IMobileMoneyPayoutFundingExecutor
{
    bool Supports(FundingSourceType sourceType);

    Task<FundingExecutionProviderResult> ExecuteAsync(
        FundingExecutionProviderRequest request,
        CancellationToken cancellationToken = default);
}
