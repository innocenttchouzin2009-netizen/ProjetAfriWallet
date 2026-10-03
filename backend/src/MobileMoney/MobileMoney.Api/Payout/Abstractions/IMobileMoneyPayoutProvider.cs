using MobileMoney.Production.Payout.Application;

namespace MobileMoney.Production.Payout.Abstractions;

public interface IMobileMoneyPayoutProvider
{
    Task<MobileMoneyPayoutSubmissionResult> SubmitAsync(
        MobileMoneyPayoutSubmission submission,
        CancellationToken cancellationToken = default);
}
