namespace MobileMoney.Production.Payout.Execution.Contracts;

public sealed record PrepareMobileMoneyPayoutExecutionRequest(
    Guid QuoteId,
    Guid FundingCorrelationId,
    string IdempotencyKey);
