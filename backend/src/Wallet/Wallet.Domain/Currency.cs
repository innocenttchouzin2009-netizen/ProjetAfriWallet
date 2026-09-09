namespace AfriWallet.Wallet.Domain;

public sealed record Currency
{
    public string Code { get; }

    private Currency(string code) => Code = code;

    public static Currency Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Currency code is required.", nameof(code));
        }

        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(character => character is < 'A' or > 'Z'))
        {
            throw new ArgumentException("Currency code must contain exactly three ASCII letters.", nameof(code));
        }

        return new Currency(normalized);
    }

    public override string ToString() => Code;
}
