namespace AfriWallet.Merchant.Application.Contracts.QrPayments;

public sealed record QrPaymentResponse(
    string PaymentId,
    string QrId,
    string MerchantId,
    string Type,
    string Code,
    string Status,
    decimal Amount,
    string Currency,
    string MerchantName,
    string Description,
    string? TransferIntentId,
    string? ReceiptId,
    string? ReceiptCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ExpiresAt);
