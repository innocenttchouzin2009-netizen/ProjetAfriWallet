namespace AfriWallet.Merchant.Application.Contracts.QrPayments;

public sealed record DecodeQrRequest(string Code);

public sealed record DecodedQrResponse(
    string Type,
    string MerchantId,
    decimal Amount,
    string Currency,
    string MerchantName,
    string Description);
