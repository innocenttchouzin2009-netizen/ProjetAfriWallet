namespace AfriWallet.Disputes.Registry.Domain.Claims;

public enum DisputeClaimStatus
{
    Draft = 0,
    Submitted = 1,
    Open = 2,
    UnderReview = 3,
    Resolved = 4,
    Closed = 5,
    Rejected = 6,
    Cancelled = 7
}
