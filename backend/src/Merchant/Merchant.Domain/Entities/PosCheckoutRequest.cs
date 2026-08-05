namespace AfriWallet.Merchant.Domain.Entities;

public sealed record PosCheckoutRequest(
    string MerchantId,
    string TerminalCode,
    decimal AmountMinor,
    string CurrencyCode,
    string Description);
