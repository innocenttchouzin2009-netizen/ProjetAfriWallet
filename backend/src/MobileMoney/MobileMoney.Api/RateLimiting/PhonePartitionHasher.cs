using System.Security.Cryptography;
using System.Text;

namespace MobileMoney.Production.RateLimiting;

public static class PhonePartitionHasher
{
    public static string Hash(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return "empty";
        }

        var normalized = phoneNumber.Trim();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes)[..16];
    }
}
