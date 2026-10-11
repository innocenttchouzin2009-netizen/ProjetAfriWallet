using MobileMoney.Production.Payout.Execution.Domain;

namespace MobileMoney.Production.Payout.Execution.Abstractions;

public interface IMobileMoneyPayoutExecutionQuoteBindingReader
{
    Task<MobileMoneyPayoutExecutionQuoteBinding?> FindByIdAsync(
        Guid quoteId,
        CancellationToken cancellationToken = default);
}
