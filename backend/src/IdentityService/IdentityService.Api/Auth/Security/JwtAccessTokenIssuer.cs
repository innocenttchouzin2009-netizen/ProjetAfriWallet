using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityService.Api.Auth.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Api.Auth.Security;

public sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly JwtAccessTokenOptions _options;
    private readonly IClock _clock;
    private readonly SigningCredentials _credentials;

    public JwtAccessTokenIssuer(JwtAccessTokenOptions options, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Audience);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.SigningKey);

        var keyBytes = Encoding.UTF8.GetBytes(options.SigningKey);
        if (keyBytes.Length < 32)
        {
            throw new ArgumentException("SigningKey must be at least 32 bytes for HS256.", nameof(options));
        }

        _options = options;
        _clock = clock;
        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256);
    }

    public string Issue(
        Guid userId,
        Guid sessionId,
        long tokenVersion,
        DateTimeOffset expiresAtUtc)
    {
        var now = _clock.UtcNow;
        if (expiresAtUtc <= now)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAtUtc), "Access token expiry must be in the future.");
        }

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim("sid", sessionId.ToString()),
            new Claim("ver", tokenVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: _credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
