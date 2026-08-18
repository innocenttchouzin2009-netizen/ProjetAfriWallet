namespace AfriWallet.Disputes.Intelligence.Domain.Findings;

public enum ProtectionRecommendation
{
    NoAction = 0,
    Monitor = 1,
    CustomerProtectionReview = 2,
    ReviewMerchant = 3,
    EscalateOperations = 4
}
