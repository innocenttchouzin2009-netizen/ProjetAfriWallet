namespace AfriWallet.Merchant.Application.Contracts.QrPayments;

public sealed record GenerateQrRequest(
    string MerchantId,
    string Type,
    decimal Amount,
    string Currency,
    string MerchantName,
    string Description);
