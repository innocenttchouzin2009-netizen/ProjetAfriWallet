namespace MobileMoney.Production.Payout.Execution.Contracts;

public sealed record PrepareMobileMoneyPayoutExecutionHttpRequest(
    Guid QuoteId,
    Guid FundingCorrelationId);
