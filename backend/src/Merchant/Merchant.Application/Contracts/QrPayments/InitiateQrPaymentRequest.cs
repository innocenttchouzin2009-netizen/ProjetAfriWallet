namespace AfriWallet.Merchant.Application.Contracts.QrPayments;

public sealed record InitiateQrPaymentRequest(
    string QrId,
    string PayerWalletId,
    decimal Amount,
    string Currency);
