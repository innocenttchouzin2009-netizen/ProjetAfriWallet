namespace AfriWallet.Merchants.Checkout.Domain.Checkout;

public enum CheckoutSessionStatus
{
    Open = 0,
    ReadyForPayment = 1,
    Cancelled = 2,
    Expired = 3
}
