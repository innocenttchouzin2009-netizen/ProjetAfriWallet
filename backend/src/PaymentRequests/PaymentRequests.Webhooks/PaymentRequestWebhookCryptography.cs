using System.Security.Cryptography;
using System.Text;

namespace AfriWallet.PaymentRequests.Webhooks;

internal static class PaymentRequestWebhookCryptography
{
    public const string SignaturePrefix = "v1=";

    public static byte[] ComputeSignature(
        ReadOnlySpan<byte> payload,
        Guid eventId,
        long timestampUnixSeconds,
        string keyId,
        string secret)
    {
        var prefix = Encoding.UTF8.GetBytes(
            $"v1.{keyId}.{timestampUnixSeconds}.{eventId:D}.");
        var canonical = new byte[prefix.Length + payload.Length];
        prefix.CopyTo(canonical, 0);
        payload.CopyTo(canonical.AsSpan(prefix.Length));

        var keyBytes = Encoding.UTF8.GetBytes(secret);
        try
        {
            using var hmac = new HMACSHA256(keyBytes);
            return hmac.ComputeHash(canonical);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyBytes);
            CryptographicOperations.ZeroMemory(canonical);
        }
    }
}
