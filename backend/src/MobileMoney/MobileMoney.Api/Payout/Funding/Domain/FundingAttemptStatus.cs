namespace MobileMoney.Production.Payout.Funding.Domain;

public enum FundingAttemptStatus
{
    Planned,
    Processing,
    Succeeded,
    Failed,
    Cancelled
}
