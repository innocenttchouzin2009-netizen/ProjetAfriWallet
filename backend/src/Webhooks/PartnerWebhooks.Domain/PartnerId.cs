namespace AfriWallet.PartnerWebhooks.Domain;

public readonly record struct PartnerId
{
    public string Value { get; }

    private PartnerId(string value) => Value = value;

    public static PartnerId From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Partner id is required.", nameof(value));
        }

        var normalized = value.Trim();
        if (normalized.Length > 128)
        {
            throw new ArgumentException("Partner id cannot exceed 128 characters.", nameof(value));
        }

        if (normalized.Any(char.IsWhiteSpace) ||
            normalized.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.')))
        {
            throw new ArgumentException("Partner id contains unsupported characters.", nameof(value));
        }

        return new PartnerId(normalized);
    }

    public override string ToString() => Value;
}
