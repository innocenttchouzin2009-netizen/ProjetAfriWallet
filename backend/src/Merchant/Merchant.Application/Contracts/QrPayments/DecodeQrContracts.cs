namespace AfriWallet.Merchant.Application.Contracts.QrPayments;

public sealed record DecodeQrRequest(string Code);

public sealed record DecodedQrResponse(
    string QrId,
    string Type,
    string MerchantId,
    long AmountMinor,
    string Currency,
    string MerchantName,
    string Description,
    string Status,
    DateTimeOffset? ExpiresAt);
