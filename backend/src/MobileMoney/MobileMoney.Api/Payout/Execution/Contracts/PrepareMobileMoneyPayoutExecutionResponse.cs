namespace MobileMoney.Production.Payout.Execution.Contracts;

public sealed record PrepareMobileMoneyPayoutExecutionResponse(
    Guid BindingId,
    Guid QuoteId,
    Guid FundingCorrelationId,
    string IdempotencyKey,
    string Status,
    bool IsReplay,
    DateTimeOffset PreparedAtUtc);
