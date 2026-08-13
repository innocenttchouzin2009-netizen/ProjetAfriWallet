using System.Security.Cryptography;
using System.Text;
using AfriWallet.BankingPlatform.BankProviderIntegration.Application.Interfaces;

namespace AfriWallet.BankingPlatform.BankProviderIntegration.Infrastructure.Security;

public sealed class HmacRequestSigner : IRequestSigner
{
    public string Sign(string payload, string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
