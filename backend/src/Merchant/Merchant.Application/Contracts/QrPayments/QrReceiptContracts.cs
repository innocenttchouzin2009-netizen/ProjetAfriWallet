namespace AfriWallet.Merchant.Application.Contracts.QrPayments;

public sealed record QrReceiptRequest(string TransferIntentId);

public sealed record QrReceiptResponse(
    string ReceiptId,
    string ReceiptCode);
