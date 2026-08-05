namespace AfriWallet.Merchant.Domain.Entities;

public sealed class MerchantCapabilities
{
    public bool QrPayments { get; set; } = true;
    public bool PosPayments { get; set; } = true;
    public bool OnlinePayments { get; set; } = true;
    public bool PaymentLinks { get; set; } = true;
    public bool RecurringPayments { get; set; } = false;
    public bool Refunds { get; set; } = true;
    public bool SplitPayments { get; set; } = false;
    public bool Subscriptions { get; set; } = false;
}
