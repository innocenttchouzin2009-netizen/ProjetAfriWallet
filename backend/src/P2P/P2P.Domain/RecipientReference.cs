namespace AfriWallet.P2P.Domain;

public sealed record RecipientReference
{
    public const int MaximumAfWalIdLength = 128;
    public const int MaximumQrTokenLength = 2048;

    public RecipientReferenceKind Kind { get; }
    public string Value { get; }

    private RecipientReference(RecipientReferenceKind kind, string value)
    {
        Kind = kind;
        Value = value;
    }

    public static RecipientReference FromAfWalId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("AfWal ID is required.", nameof(value));
        }

        var normalized = value.Trim();
        if (normalized.Length > MaximumAfWalIdLength)
        {
            throw new ArgumentException($"AfWal ID cannot exceed {MaximumAfWalIdLength} characters.", nameof(value));
        }

        if (normalized.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("AfWal ID cannot contain whitespace.", nameof(value));
        }

        return new RecipientReference(RecipientReferenceKind.AfWalId, normalized);
    }

    public static RecipientReference FromQrToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("QR recipient token is required.", nameof(value));
        }

        if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("QR recipient token cannot contain leading or trailing whitespace.", nameof(value));
        }

        if (value.Length > MaximumQrTokenLength)
        {
            throw new ArgumentException($"QR recipient token cannot exceed {MaximumQrTokenLength} characters.", nameof(value));
        }

        return new RecipientReference(RecipientReferenceKind.QrToken, value);
    }
}
