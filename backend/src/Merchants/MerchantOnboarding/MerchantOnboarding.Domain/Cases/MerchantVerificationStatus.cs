namespace AfriWallet.Merchants.Onboarding.Domain.Cases;

public enum MerchantVerificationStatus
{
    Created = 0,
    PendingDocuments = 1,
    ReadyForReview = 2,
    UnderReview = 3,
    ManualReviewRequired = 4,
    Verified = 5,
    Rejected = 6,
    Closed = 7
}
