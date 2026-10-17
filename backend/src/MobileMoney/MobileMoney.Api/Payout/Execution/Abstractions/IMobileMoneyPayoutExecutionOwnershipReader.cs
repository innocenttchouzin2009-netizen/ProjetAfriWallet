namespace MobileMoney.Production.Payout.Execution.Abstractions;

public interface IMobileMoneyPayoutExecutionOwnershipReader
{
    Task<Guid?> FindQuoteOwnerUserIdAsync(
        Guid quoteId,
        CancellationToken cancellationToken = default);

    Task<Guid?> FindFundingOwnerUserIdAsync(
        Guid fundingCorrelationId,
        CancellationToken cancellationToken = default);
}
