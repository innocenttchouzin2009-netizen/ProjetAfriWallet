using IdentityService.Api.Auth.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Api.Auth.Security;

public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private static readonly object Marker = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return _hasher.HashPassword(Marker, password);
    }

    public bool Verify(string password, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        var result = _hasher.VerifyHashedPassword(Marker, passwordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
