namespace AfriWallet.Disputes.Eligibility.Domain.Classification;

public enum DisputeCategory
{
    UnauthorizedTransaction = 0,
    ProcessingError = 1,
    MerchantService = 2,
    RefundIssue = 3,
    CashWithdrawal = 4,
    BankTransfer = 5,
    FraudRelated = 6,
    Other = 7
}
