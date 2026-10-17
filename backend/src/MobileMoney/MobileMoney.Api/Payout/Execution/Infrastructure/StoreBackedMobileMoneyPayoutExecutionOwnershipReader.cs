using MobileMoney.Production.Payout.Execution.Abstractions;
using MobileMoney.Production.Payout.Funding.Abstractions;
using MobileMoney.Production.Payout.Quote.Abstractions;

namespace MobileMoney.Production.Payout.Execution.Infrastructure;

public sealed class StoreBackedMobileMoneyPayoutExecutionOwnershipReader(
    IMobileMoneyPayoutQuoteStore quoteStore,
    IMobileMoneyPayoutFundingAttemptStore fundingStore)
    : IMobileMoneyPayoutExecutionOwnershipReader
{
    public Task<Guid?> FindQuoteOwnerUserIdAsync(
        Guid quoteId,
        CancellationToken cancellationToken = default) =>
        quoteStore.FindOwnerUserIdAsync(quoteId, cancellationToken);

    public Task<Guid?> FindFundingOwnerUserIdAsync(
        Guid fundingCorrelationId,
        CancellationToken cancellationToken = default) =>
        fundingStore.FindOwnerUserIdAsync(
            fundingCorrelationId,
            cancellationToken);
}
