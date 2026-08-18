namespace AfriWallet.Disputes.Registry.Domain.Evidence;

public enum DisputeEvidenceType
{
    CustomerStatement = 0,
    Receipt = 1,
    MerchantResponse = 2,
    BankStatement = 3,
    PaymentRecord = 4,
    FraudFinding = 5,
    ComplianceRecord = 6,
    AnalystNote = 7
}
