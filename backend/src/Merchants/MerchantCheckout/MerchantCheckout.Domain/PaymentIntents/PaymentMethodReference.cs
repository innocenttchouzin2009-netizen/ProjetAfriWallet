namespace AfriWallet.Merchants.Checkout.Domain.PaymentIntents;

public sealed record PaymentMethodReference
{
    public PaymentMethodReference(string type, string tokenReference)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Payment method type is required.", nameof(type));
        if (string.IsNullOrWhiteSpace(tokenReference))
            throw new ArgumentException("Payment method token reference is required.", nameof(tokenReference));
        if (tokenReference.Length > 200)
            throw new ArgumentException("Payment method token reference is too long.", nameof(tokenReference));

        Type = type.Trim();
        TokenReference = tokenReference.Trim();
    }

    public string Type { get; }
    public string TokenReference { get; }
}
