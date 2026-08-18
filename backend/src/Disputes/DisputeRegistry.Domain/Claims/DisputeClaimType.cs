namespace AfriWallet.Disputes.Registry.Domain.Claims;

public enum DisputeClaimType
{
    TransactionNotRecognized = 0,
    DuplicateCharge = 1,
    WrongAmount = 2,
    ServiceNotReceived = 3,
    GoodsNotReceived = 4,
    RefundNotReceived = 5,
    CashWithdrawalDispute = 6,
    BankTransferDispute = 7,
    MerchantDispute = 8,
    FraudRelated = 9,
    Other = 10
}
