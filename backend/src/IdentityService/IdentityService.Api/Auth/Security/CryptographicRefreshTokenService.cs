using System.Security.Cryptography;
using IdentityService.Api.Auth.Abstractions;

namespace IdentityService.Api.Auth.Security;

public sealed class CryptographicRefreshTokenService : IRefreshTokenService
{
    private const int TokenBytes = 32;

    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenBytes);
        return Base64UrlEncode(bytes);
    }

    public string Hash(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        var bytes = System.Text.Encoding.UTF8.GetBytes(refreshToken);
        var hash = SHA256.HashData(bytes);
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
