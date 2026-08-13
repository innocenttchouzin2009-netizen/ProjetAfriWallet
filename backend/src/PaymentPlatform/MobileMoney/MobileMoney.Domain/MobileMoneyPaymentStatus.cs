namespace AfriWallet.PaymentPlatform.MobileMoney.Domain;

public enum MobileMoneyPaymentStatus
{
    Pending,
    Processing,
    Succeeded,
    Failed,
    Cancelled,
    Expired
}