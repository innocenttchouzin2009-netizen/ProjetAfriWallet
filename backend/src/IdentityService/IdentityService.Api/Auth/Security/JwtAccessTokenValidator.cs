using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Api.Auth.Security;

public sealed class JwtAccessTokenValidator
{
    private readonly JwtSecurityTokenHandler _handler = new() { MapInboundClaims = false };
    private readonly TokenValidationParameters _validationParameters;

    public JwtAccessTokenValidator(JwtAccessTokenOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Audience);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.SigningKey);

        var keyBytes = Encoding.UTF8.GetBytes(options.SigningKey);
        if (keyBytes.Length < 32)
        {
            throw new ArgumentException("SigningKey must be at least 32 bytes for HS256.", nameof(options));
        }

        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    public AuthAccessTokenPrincipal? Validate(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        try
        {
            var principal = _handler.ValidateToken(accessToken, _validationParameters, out var validatedToken);
            if (validatedToken is not JwtSecurityToken jwt ||
                !string.Equals(jwt.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.Ordinal))
            {
                return null;
            }

            var userIdValue = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var sessionIdValue = principal.FindFirst("sid")?.Value;
            var tokenVersionValue = principal.FindFirst("ver")?.Value;

            return Guid.TryParse(userIdValue, out var userId) &&
                   Guid.TryParse(sessionIdValue, out var sessionId) &&
                   long.TryParse(tokenVersionValue, System.Globalization.CultureInfo.InvariantCulture, out var tokenVersion)
                ? new AuthAccessTokenPrincipal(userId, sessionId, tokenVersion)
                : null;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
