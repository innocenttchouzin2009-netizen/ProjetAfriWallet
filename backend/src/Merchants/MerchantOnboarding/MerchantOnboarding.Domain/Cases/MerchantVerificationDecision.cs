namespace AfriWallet.Merchants.Onboarding.Domain.Cases;

public enum MerchantVerificationDecision
{
    None = 0,
    Verified = 1,
    Rejected = 2,
    ManualReviewRequired = 3
}
