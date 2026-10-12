namespace MobileMoney.Production.Payout.Execution.Contracts;

public static class MobileMoneyPayoutExecutionErrorCodes
{
    public const string QuoteNotFound = "PAYOUT_EXECUTION_QUOTE_NOT_FOUND";
    public const string FundingNotFound = "PAYOUT_EXECUTION_FUNDING_NOT_FOUND";
    public const string IdempotencyConflict = "PAYOUT_EXECUTION_IDEMPOTENCY_CONFLICT";
    public const string BindingInvalid = "PAYOUT_EXECUTION_BINDING_INVALID";
}
