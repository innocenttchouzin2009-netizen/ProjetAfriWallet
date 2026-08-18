namespace AfriWallet.Merchants.Checkout.Domain.PaymentIntents;

public enum PaymentIntentStatus
{
    Created = 0,
    RequiresPaymentMethod = 1,
    ReadyForAuthorization = 2,
    Cancelled = 3,
    Expired = 4
}
