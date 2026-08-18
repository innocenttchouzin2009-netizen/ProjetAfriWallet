namespace AfriWallet.Merchants.Registry.Domain.Merchants;

/// Administrative registry status only; never a payment authorization.
public enum MerchantStatus
{
    Draft = 0,
    Registered = 1,
    PendingVerification = 2,
    Active = 3,
    Suspended = 4,
    Closed = 5
}
