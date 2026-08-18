namespace AfriWallet.Merchants.Checkout.Domain.Checkout;

public sealed class CheckoutSession
{
    public CheckoutSession(Guid checkoutSessionId, string merchantId, string? customerReference, string? merchantOrderReference, long amountMinor, string currency, string returnUrl, CheckoutMetadata metadata, DateTimeOffset expiresAtUtc, DateTimeOffset createdAtUtc)
    {
        if (checkoutSessionId == Guid.Empty)
            throw new ArgumentException("Checkout session id is required.", nameof(checkoutSessionId));
        if (string.IsNullOrWhiteSpace(merchantId))
            throw new ArgumentException("Merchant id is required.", nameof(merchantId));
        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor));
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
            throw new ArgumentException("Currency must be ISO-4217 style.", nameof(currency));
        if (string.IsNullOrWhiteSpace(returnUrl))
            throw new ArgumentException("Return URL is required.", nameof(returnUrl));
        if (expiresAtUtc <= createdAtUtc)
            throw new ArgumentException("Checkout session expiry must be in the future.", nameof(expiresAtUtc));

        CheckoutSessionId = checkoutSessionId;
        MerchantId = merchantId.Trim();
        CustomerReference = customerReference?.Trim();
        MerchantOrderReference = merchantOrderReference?.Trim();
        AmountMinor = amountMinor;
        Currency = currency.Trim().ToUpperInvariant();
        ReturnUrl = returnUrl.Trim();
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        Status = CheckoutSessionStatus.Open;
    }

    public Guid CheckoutSessionId { get; }
    public string MerchantId { get; }
    public string? CustomerReference { get; }
    public string? MerchantOrderReference { get; }
    public long AmountMinor { get; }
    public string Currency { get; }
    public string ReturnUrl { get; }
    public CheckoutMetadata Metadata { get; }
    public CheckoutSessionStatus Status { get; private set; }
    public Guid? PaymentIntentId { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void AttachPaymentIntent(Guid paymentIntentId, DateTimeOffset now)
    {
        EnsureMutable(now);
        if (paymentIntentId == Guid.Empty)
            throw new ArgumentException("Payment intent id is required.", nameof(paymentIntentId));
        if (PaymentIntentId.HasValue)
            throw new InvalidOperationException("Checkout already has a payment intent.");

        PaymentIntentId = paymentIntentId;
        UpdatedAtUtc = now;
    }

    public void MarkReadyForPayment(DateTimeOffset now)
    {
        EnsureMutable(now);
        if (!PaymentIntentId.HasValue)
            throw new InvalidOperationException("Checkout requires a payment intent.");

        Status = CheckoutSessionStatus.ReadyForPayment;
        UpdatedAtUtc = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        EnsureMutable(now);
        Status = CheckoutSessionStatus.Cancelled;
        UpdatedAtUtc = now;
    }

    public void Expire(DateTimeOffset now)
    {
        if (Status is CheckoutSessionStatus.Cancelled or CheckoutSessionStatus.Expired)
            throw new InvalidOperationException("Terminal checkout session cannot expire again.");

        Status = CheckoutSessionStatus.Expired;
        UpdatedAtUtc = now;
    }

    private void EnsureMutable(DateTimeOffset now)
    {
        if (Status is CheckoutSessionStatus.Cancelled or CheckoutSessionStatus.Expired)
            throw new InvalidOperationException("Terminal checkout session is immutable.");
        if (now >= ExpiresAtUtc)
            throw new InvalidOperationException("Checkout session has expired.");
    }
}
