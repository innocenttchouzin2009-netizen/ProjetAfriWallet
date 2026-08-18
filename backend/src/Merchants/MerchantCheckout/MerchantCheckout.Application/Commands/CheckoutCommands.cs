namespace AfriWallet.Merchants.Checkout.Application.Commands;

public sealed record CreateCheckoutSessionCommand(string MerchantId, string? CustomerReference, string? MerchantOrderReference, long AmountMinor, string Currency, string ReturnUrl, IReadOnlyDictionary<string, string>? Metadata, int ExpiresInMinutes, string PaymentIntentIdempotencyKey, string Actor);
public sealed record AttachCheckoutPaymentMethodCommand(Guid CheckoutSessionId, string PaymentMethodType, string TokenReference, string Actor);
public sealed record CancelCheckoutSessionCommand(Guid CheckoutSessionId, string Actor);
public sealed record ExpireCheckoutSessionCommand(Guid CheckoutSessionId, string Actor);
