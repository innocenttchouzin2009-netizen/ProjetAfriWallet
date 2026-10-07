namespace AfriWallet.Transfer.Domain.Funding;

public enum FundingAttemptStatus
{
    Planned,
    Processing,
    Succeeded,
    Failed,
    Cancelled
}
