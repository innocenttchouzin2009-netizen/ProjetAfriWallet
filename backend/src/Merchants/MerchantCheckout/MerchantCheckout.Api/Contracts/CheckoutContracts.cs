namespace AfriWallet.Merchants.Checkout.Api.Contracts;

public sealed record CreateCheckoutRequest(string MerchantId, string? CustomerReference, string? MerchantOrderReference, long AmountMinor, string Currency, string ReturnUrl, IReadOnlyDictionary<string, string>? Metadata, int ExpiresInMinutes, string IdempotencyKey);
public sealed record AttachPaymentMethodRequest(string PaymentMethodType, string TokenReference);
