namespace AfriWallet.Fx.Domain;

public sealed record CurrencyCode
{
    public string Value { get; }

    private CurrencyCode(string value) => Value = value;

    public static CurrencyCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Currency code is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(character => character is < 'A' or > 'Z'))
        {
            throw new ArgumentException("Currency code must contain exactly three ASCII letters.", nameof(value));
        }

        return new CurrencyCode(normalized);
    }

    public override string ToString() => Value;
}
