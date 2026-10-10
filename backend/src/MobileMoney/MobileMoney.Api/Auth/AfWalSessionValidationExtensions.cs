using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace MobileMoney.Production.Auth;

public static class AfWalSessionValidationExtensions
{
    private const string ErrorCodeItem =
        "AfWalSessionValidation.ErrorCode";
    private const string ErrorMessageItem =
        "AfWalSessionValidation.ErrorMessage";

    public static IServiceCollection AddAfWalSessionValidation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var validationOptions =
            AfWalSessionValidationOptions.FromConfiguration(configuration);

        services.AddSingleton(validationOptions);
        services.AddHttpClient<
            IAfWalSessionValidator,
            HttpAfWalSessionValidator>(client =>
        {
            client.BaseAddress =
                validationOptions.IdentityServiceBaseUri;
            client.Timeout = validationOptions.Timeout;
        });

        services
            .AddAuthentication(
                JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(
                                    validationOptions.SigningKey)),
                        ValidateIssuer = true,
                        ValidIssuer = validationOptions.Issuer,
                        ValidateAudience = true,
                        ValidAudience = validationOptions.Audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                options.Events = CreateEvents();
            });

        services.AddAuthorization();
        return services;
    }

    private static JwtBearerEvents CreateEvents() =>
        new()
        {
            OnTokenValidated = ValidateSessionAsync,
            OnChallenge = WriteChallengeAsync,
            OnForbidden = WriteForbiddenAsync
        };

    private static async Task ValidateSessionAsync(
        TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (principal is null ||
            !Guid.TryParse(
                principal.FindFirst("sub")?.Value,
                out var userId) ||
            !Guid.TryParse(
                principal.FindFirst("sid")?.Value,
                out var sessionId) ||
            !long.TryParse(
                principal.FindFirst("ver")?.Value,
                out var tokenVersion) ||
            !TryReadBearerToken(
                context.Request,
                out var accessToken))
        {
            Fail(
                context,
                AfWalSessionValidationErrorCode.TokenInvalid,
                "Invalid access token claims.");
            return;
        }

        var validator =
            context.HttpContext.RequestServices
                .GetRequiredService<IAfWalSessionValidator>();

        var result = await validator.ValidateAsync(
            accessToken,
            userId,
            sessionId,
            tokenVersion,
            context.HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            Fail(
                context,
                result.ErrorCode ??
                    AfWalSessionValidationErrorCode.TokenInvalid,
                result.ErrorMessage ??
                    "Invalid AfWal session.");
        }
    }

    private static Task WriteChallengeAsync(
        JwtBearerChallengeContext context)
    {
        context.HandleResponse();

        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        var code =
            context.HttpContext.Items[ErrorCodeItem] as string ??
            AfWalSessionValidationErrorCode.TokenInvalid;
        var message =
            context.HttpContext.Items[ErrorMessageItem] as string ??
            "Invalid or missing AfWal access token.";

        return WriteErrorAsync(
            context.HttpContext,
            StatusCodes.Status401Unauthorized,
            code,
            message);
    }

    private static Task WriteForbiddenAsync(
        ForbiddenContext context) =>
        WriteErrorAsync(
            context.HttpContext,
            StatusCodes.Status403Forbidden,
            AfWalSessionValidationErrorCode.Forbidden,
            "Access is forbidden.");

    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            new AfWalAuthErrorResponse(
                code,
                message,
                context.TraceIdentifier),
            cancellationToken: context.RequestAborted);
    }

    private static void Fail(
        TokenValidatedContext context,
        string code,
        string message)
    {
        context.HttpContext.Items[ErrorCodeItem] = code;
        context.HttpContext.Items[ErrorMessageItem] = message;
        context.Fail(message);
    }

    private static bool TryReadBearerToken(
        HttpRequest request,
        out string accessToken)
    {
        const string prefix = "Bearer ";
        var authorization =
            request.Headers.Authorization.ToString();

        if (!authorization.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase))
        {
            accessToken = string.Empty;
            return false;
        }

        accessToken = authorization[prefix.Length..].Trim();
        return accessToken.Length > 0;
    }

    private sealed record AfWalAuthErrorResponse(
        string Code,
        string Message,
        string TraceId);
}
