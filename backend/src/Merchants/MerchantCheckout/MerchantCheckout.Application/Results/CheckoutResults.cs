using AfriWallet.Merchants.Checkout.Domain.Checkout;
using AfriWallet.Merchants.Checkout.Domain.PaymentIntents;

namespace AfriWallet.Merchants.Checkout.Application.Results;

public sealed record CheckoutSessionResult(Guid CheckoutSessionId, string MerchantId, string? CustomerReference, string? MerchantOrderReference, long AmountMinor, string Currency, string ReturnUrl, CheckoutSessionStatus Status, Guid PaymentIntentId, DateTimeOffset ExpiresAtUtc, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);
public sealed record PaymentIntentResult(Guid PaymentIntentId, Guid CheckoutSessionId, string MerchantId, long AmountMinor, string Currency, PaymentIntentStatus Status, string? PaymentMethodType, DateTimeOffset ExpiresAtUtc, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);
