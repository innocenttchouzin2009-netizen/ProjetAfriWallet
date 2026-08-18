namespace AfriWallet.Disputes.Eligibility.Domain.Eligibility;

public enum DisputeEligibilityReason
{
    Eligible = 0,
    ClaimNotFound = 1,
    TransactionNotFound = 2,
    AwidMismatch = 3,
    CurrencyMismatch = 4,
    ClaimAmountExceedsTransaction = 5,
    SubmissionWindowExpired = 6,
    TransactionNotCompleted = 7,
    UnsupportedClaimType = 8,
    ManualReviewRequired = 9
}
