namespace AfriWallet.Merchant.Application.Contracts.QrPayments;

public static class QrPaymentContractValues
{
    public static class Types
    {
        public const string Static = "Static";
        public const string Dynamic = "Dynamic";
    }

    public static class Statuses
    {
        public const string Active = "Active";
        public const string Initiated = "Initiated";
        public const string Paid = "Paid";
        public const string Expired = "Expired";
    }
}
