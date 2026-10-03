namespace AfriWallet.Merchant.Application.Contracts.QrPayments;

public sealed record QrPaymentStatusResponse(
    string PaymentId,
    string QrId,
    string TransferIntentId,
    string Status,
    long AmountMinor,
    string Currency,
    string? ReceiptId,
    string? ReceiptCode,
    DateTimeOffset UpdatedAt);
