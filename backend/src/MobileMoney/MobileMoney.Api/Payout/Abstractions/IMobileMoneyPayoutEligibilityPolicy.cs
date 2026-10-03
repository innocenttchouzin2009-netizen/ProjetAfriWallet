using MobileMoney.Production.Payout.Application;
using MobileMoney.Production.Payout.Domain;

namespace MobileMoney.Production.Payout.Abstractions;

public interface IMobileMoneyPayoutEligibilityPolicy
{
    Task<MobileMoneyPayoutEligibilityResult> EvaluateAsync(
        MobileMoneyPayoutCorridor corridor,
        CancellationToken cancellationToken = default);
}
