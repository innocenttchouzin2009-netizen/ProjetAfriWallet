namespace AfriWallet.Merchants.Checkout.Domain.PaymentIntents;

public sealed class PaymentIntent
{
    public PaymentIntent(Guid paymentIntentId, Guid checkoutSessionId, string merchantId, long amountMinor, string currency, string idempotencyKey, DateTimeOffset expiresAtUtc, DateTimeOffset createdAtUtc)
    {
        if (paymentIntentId == Guid.Empty || checkoutSessionId == Guid.Empty)
            throw new ArgumentException("Payment intent and checkout session ids are required.");
        if (string.IsNullOrWhiteSpace(merchantId))
            throw new ArgumentException("Merchant id is required.", nameof(merchantId));
        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor));
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
            throw new ArgumentException("Currency must be ISO-4217 style.", nameof(currency));
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
        if (expiresAtUtc <= createdAtUtc)
            throw new ArgumentException("Payment intent expiry must be in the future.", nameof(expiresAtUtc));

        PaymentIntentId = paymentIntentId;
        CheckoutSessionId = checkoutSessionId;
        MerchantId = merchantId.Trim();
        AmountMinor = amountMinor;
        Currency = currency.Trim().ToUpperInvariant();
        IdempotencyKey = idempotencyKey.Trim();
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        Status = PaymentIntentStatus.RequiresPaymentMethod;
    }

    public Guid PaymentIntentId { get; }
    public Guid CheckoutSessionId { get; }
    public string MerchantId { get; }
    public long AmountMinor { get; }
    public string Currency { get; }
    public string IdempotencyKey { get; }
    public PaymentIntentStatus Status { get; private set; }
    public PaymentMethodReference? PaymentMethod { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void AttachPaymentMethod(PaymentMethodReference paymentMethod, DateTimeOffset now)
    {
        EnsureMutable(now);
        if (Status != PaymentIntentStatus.RequiresPaymentMethod)
            throw new InvalidOperationException("Payment intent is not waiting for a payment method.");

        PaymentMethod = paymentMethod ?? throw new ArgumentNullException(nameof(paymentMethod));
        Status = PaymentIntentStatus.ReadyForAuthorization;
        UpdatedAtUtc = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        EnsureMutable(now);
        Status = PaymentIntentStatus.Cancelled;
        UpdatedAtUtc = now;
    }

    public void Expire(DateTimeOffset now)
    {
        if (Status is PaymentIntentStatus.Cancelled or PaymentIntentStatus.Expired)
            throw new InvalidOperationException("Terminal payment intent cannot expire again.");

        Status = PaymentIntentStatus.Expired;
        UpdatedAtUtc = now;
    }

    private void EnsureMutable(DateTimeOffset now)
    {
        if (Status is PaymentIntentStatus.Cancelled or PaymentIntentStatus.Expired)
            throw new InvalidOperationException("Terminal payment intent is immutable.");
        if (now >= ExpiresAtUtc)
            throw new InvalidOperationException("Payment intent has expired.");
    }
}
