namespace MobileMoney.Production.Payout.Application;

public sealed class MobileMoneyPayoutIdempotencyConflictException
    : InvalidOperationException
{
    public const string ConflictCode = "PAYOUT_IDEMPOTENCY_CONFLICT";

    public MobileMoneyPayoutIdempotencyConflictException()
        : base("Idempotency key is already bound to a different payout request.")
    {
    }

    public string Code => ConflictCode;
}
