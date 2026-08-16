namespace AfriWallet.Fraud.TransactionFraud.Domain.Factors;

public enum TransactionFraudFactorType
{
    UnusualAmount = 0,
    NewBeneficiary = 1,
    HighTransactionVelocity = 2,
    RecentDeviceChange = 3,
    DeviceRisk = 4,
    FailedThenSuccessfulPayment = 5,
    GeographicAnomaly = 6,
    RepeatedAttempts = 7
}
