using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace MobileMoney.Production.Authentication;

public static class MobileMoneyAuthenticationExtensions
{
    public static IServiceCollection AddMobileMoneyJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtIssuer = configuration["Auth:Jwt:Issuer"] ?? "https://identity.afrikawallet.local";
        var jwtAudience = configuration["Auth:Jwt:Audience"] ?? "afrikawallet-mobile";
        var jwtSigningKey = configuration["Auth:Jwt:SigningKey"] ??
            Environment.GetEnvironmentVariable("AFW_AUTH_JWT_SIGNING_KEY") ??
            throw new InvalidOperationException("Auth JWT signing key is not configured for MobileMoney API.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
        return services;
    }
}
