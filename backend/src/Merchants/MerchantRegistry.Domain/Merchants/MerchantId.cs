namespace AfriWallet.Merchants.Registry.Domain.Merchants;

public readonly record struct MerchantId
{
    public MerchantId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Merchant id is required.", nameof(value));

        var normalized = value.Trim().ToUpperInvariant();
        if (!normalized.StartsWith("AFM-", StringComparison.Ordinal))
            throw new ArgumentException("Merchant id must start with AFM-.", nameof(value));

        Value = normalized;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
