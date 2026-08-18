namespace AfriWallet.Disputes.Investigation.Domain.Evidence;

public enum EvidenceType
{
    CustomerStatement = 0,
    MerchantReceipt = 1,
    DeliveryProof = 2,
    RefundProof = 3,
    TransactionRecord = 4,
    FraudDecision = 5,
    BankTransferReference = 6,
    Screenshot = 7,
    Document = 8,
    Other = 9
}
