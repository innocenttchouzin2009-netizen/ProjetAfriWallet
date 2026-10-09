using System.Security.Cryptography;
using System.Text;

namespace MobileMoney.Production.Payout.Domain;

public readonly record struct RequestFingerprint
{
    private const int Sha256HexLength = 64;

    private RequestFingerprint(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static RequestFingerprint FromCanonicalPayload(string canonicalPayload)
    {
        if (string.IsNullOrWhiteSpace(canonicalPayload))
            throw new ArgumentException(
                "Canonical payload is required.",
                nameof(canonicalPayload));

        var bytes = Encoding.UTF8.GetBytes(canonicalPayload);
        var hash = SHA256.HashData(bytes);

        return new RequestFingerprint(
            Convert.ToHexString(hash).ToLowerInvariant());
    }

    public static RequestFingerprint Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "Request fingerprint is required.",
                nameof(value));

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length != Sha256HexLength
            || normalized.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                "Request fingerprint must be a SHA-256 hexadecimal value.",
                nameof(value));
        }

        return new RequestFingerprint(normalized);
    }

    public override string ToString() => Value;
}
