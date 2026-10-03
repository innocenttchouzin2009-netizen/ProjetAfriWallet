namespace AfriWallet.Merchant.Application.Contracts.QrPayments;

public sealed record InitiateQrPaymentRequest(
    string QrId,
    string PayerWalletId,
    long AmountMinor,
    string Currency,
    string IdempotencyKey,
    Guid AuthenticatedUserId);
