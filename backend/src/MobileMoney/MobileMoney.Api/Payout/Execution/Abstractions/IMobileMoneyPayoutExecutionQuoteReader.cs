using MobileMoney.Production.Payout.Execution.Domain;

namespace MobileMoney.Production.Payout.Execution.Abstractions;

public interface IMobileMoneyPayoutExecutionQuoteReader
{
    Task<MobileMoneyPayoutExecutionQuote?> FindAsync(
        Guid quoteId,
        CancellationToken cancellationToken = default);
}
