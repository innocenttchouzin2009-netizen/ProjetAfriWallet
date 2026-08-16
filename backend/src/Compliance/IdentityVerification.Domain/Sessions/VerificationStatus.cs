namespace AfriWallet.Compliance.IdentityVerification.Domain.Sessions;

public enum VerificationStatus
{
    Created = 0,
    Submitted = 1,
    Processing = 2,
    Verified = 3,
    Rejected = 4,
    Failed = 5,
    Expired = 6,
    Cancelled = 7
}
