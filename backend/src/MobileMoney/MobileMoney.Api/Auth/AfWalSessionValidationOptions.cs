namespace MobileMoney.Production.Auth;

public sealed record AfWalSessionValidationOptions(
    string Issuer,
    string Audience,
    string SigningKey,
    Uri IdentityServiceBaseUri,
    TimeSpan Timeout)
{
    public static AfWalSessionValidationOptions FromConfiguration(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var issuer =
            configuration["Auth:Jwt:Issuer"] ??
            "https://identity.afrikawallet.local";
        var audience =
            configuration["Auth:Jwt:Audience"] ??
            "afrikawallet-mobile";
        var signingKey =
            configuration["Auth:Jwt:SigningKey"] ??
            Environment.GetEnvironmentVariable("AFW_AUTH_JWT_SIGNING_KEY");

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                "AfWal JWT signing key is not configured.");
        }

        var identityBaseUrl =
            configuration["Auth:IdentityService:BaseUrl"] ??
            Environment.GetEnvironmentVariable("AFW_IDENTITY_BASE_URL") ??
            "http://localhost:5000";

        if (!Uri.TryCreate(
                identityBaseUrl,
                UriKind.Absolute,
                out var identityServiceBaseUri))
        {
            throw new InvalidOperationException(
                "AfWal identity service base URL is invalid.");
        }

        var timeoutSeconds = 5;
        var configuredTimeout =
            configuration["Auth:IdentityService:TimeoutSeconds"];
        if (!string.IsNullOrWhiteSpace(configuredTimeout) &&
            (!int.TryParse(configuredTimeout, out timeoutSeconds) ||
             timeoutSeconds <= 0))
        {
            throw new InvalidOperationException(
                "AfWal session validation timeout must be a positive integer.");
        }

        return new AfWalSessionValidationOptions(
            issuer,
            audience,
            signingKey,
            identityServiceBaseUri,
            TimeSpan.FromSeconds(timeoutSeconds));
    }
}
