namespace MobileMoney.Production.Payout.Domain;

public enum MobileMoneyPayoutStatus
{
    Created = 0,
    Processing = 1,
    Submitted = 2,
    Succeeded = 3,
    Failed = 4,
    Cancelled = 5
}
