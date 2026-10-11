namespace MobileMoney.Production.Payout.Execution.Domain;

public static class MobileMoneyPayoutExecutionErrorCodes
{
    public const string QuoteNotFound = "PAYOUT_QUOTE_NOT_FOUND";
    public const string QuoteExpired = "PAYOUT_QUOTE_EXPIRED";
    public const string QuoteBindingMismatch = "PAYOUT_QUOTE_BINDING_MISMATCH";
    public const string IdempotencyConflict = "PAYOUT_IDEMPOTENCY_CONFLICT";
}
