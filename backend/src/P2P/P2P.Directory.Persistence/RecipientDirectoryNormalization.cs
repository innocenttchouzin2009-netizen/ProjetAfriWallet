using System.Security.Cryptography;
using System.Text;
using AfriWallet.P2P.Domain;

namespace AfriWallet.P2P.Directory.Persistence;

public static class RecipientDirectoryNormalization
{
    public static string NormalizeAfWalId(string afWalId)
    {
        var reference = RecipientReference.FromAfWalId(afWalId);
        return reference.Value;
    }

    public static string HashQrToken(string qrToken)
    {
        var reference = RecipientReference.FromQrToken(qrToken);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(reference.Value));
        return Convert.ToHexString(bytes);
    }
}
