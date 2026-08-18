namespace AfriWallet.Merchants.Registry.Domain.Merchants;

/// Declarative only in AFW-DLV-0019.1; does not activate any capability.
public enum MerchantCapability
{
    InPersonPayments = 0,
    OnlinePayments = 1,
    QrPayments = 2,
    PaymentLinks = 3,
    Refunds = 4,
    Payouts = 5
}
