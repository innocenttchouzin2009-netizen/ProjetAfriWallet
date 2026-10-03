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

    public static class ErrorCodes
    {
        public const string Validation = "QR_PAYMENT_VALIDATION_ERROR";
        public const string NotFound = "QR_PAYMENT_NOT_FOUND";
        public const string NotActive = "QR_PAYMENT_NOT_ACTIVE";
        public const string Expired = "QR_PAYMENT_EXPIRED";
        public const string IdempotencyConflict = "QR_PAYMENT_IDEMPOTENCY_CONFLICT";
        public const string Unauthorized = "QR_PAYMENT_UNAUTHORIZED";
        public const string PayerWalletNotFound = "QR_PAYMENT_PAYER_WALLET_NOT_FOUND";
        public const string PayerWalletUnavailable = "QR_PAYMENT_PAYER_WALLET_UNAVAILABLE";
    }
}
