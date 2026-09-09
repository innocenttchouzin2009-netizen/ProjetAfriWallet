namespace AfriWallet.Wallet.Domain;

public sealed record CountryCode
{
    public string Value { get; }

    private CountryCode(string value) => Value = value;

    public static CountryCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Country code is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 2 || normalized.Any(character => character is < 'A' or > 'Z'))
        {
            throw new ArgumentException("Country code must contain exactly two ASCII letters.", nameof(value));
        }

        return new CountryCode(normalized);
    }

    public override string ToString() => Value;
}
