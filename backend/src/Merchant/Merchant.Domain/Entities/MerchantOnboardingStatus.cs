namespace AfriWallet.Merchant.Domain.Entities;

public enum MerchantOnboardingStatus
{
    Draft,
    ProfileCompleted,
    DocumentsSubmitted,
    KycInProgress,
    KycApproved,
    KycRejected,
    Active,
    Suspended
}