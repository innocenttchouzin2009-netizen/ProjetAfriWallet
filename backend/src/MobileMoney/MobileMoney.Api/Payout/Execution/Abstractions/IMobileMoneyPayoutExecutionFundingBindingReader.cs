using MobileMoney.Production.Payout.Execution.Domain;

namespace MobileMoney.Production.Payout.Execution.Abstractions;

public interface IMobileMoneyPayoutExecutionFundingBindingReader
{
    Task<MobileMoneyPayoutExecutionFundingBinding?> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);
}
