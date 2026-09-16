using System.Security.Cryptography;
using System.Text;

namespace AfriWallet.Notifications.Persistence;

public interface IPushTokenProtector
{
    string Protect(string token);
    string Unprotect(string protectedToken);
}

public sealed class AesGcmPushTokenProtector : IPushTokenProtector
{
    private readonly byte[] key;

    public AesGcmPushTokenProtector(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length != 32)
        {
            throw new ArgumentException("Push token protection key must be exactly 32 bytes.", nameof(key));
        }

        this.key = key.ToArray();
    }

    public string Protect(string token)
    {
        if (string.IsNullOrEmpty(token)) throw new ArgumentException("Push token is required.", nameof(token));

        var plaintext = Encoding.UTF8.GetBytes(token);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var ciphertext = new byte[plaintext.Length];

        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var payload = new byte[1 + nonce.Length + tag.Length + ciphertext.Length];
        payload[0] = 1;
        Buffer.BlockCopy(nonce, 0, payload, 1, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, 1 + nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, payload, 1 + nonce.Length + tag.Length, ciphertext.Length);
        CryptographicOperations.ZeroMemory(plaintext);
        return Convert.ToBase64String(payload);
    }

    public string Unprotect(string protectedToken)
    {
        if (string.IsNullOrWhiteSpace(protectedToken)) throw new ArgumentException("Protected push token is required.", nameof(protectedToken));

        var payload = Convert.FromBase64String(protectedToken);
        if (payload.Length < 30 || payload[0] != 1)
        {
            throw new CryptographicException("Protected push token payload is invalid.");
        }

        var nonce = payload.AsSpan(1, 12).ToArray();
        var tag = payload.AsSpan(13, 16).ToArray();
        var ciphertext = payload.AsSpan(29).ToArray();
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, tag.Length);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        try
        {
            return Encoding.UTF8.GetString(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }
}
