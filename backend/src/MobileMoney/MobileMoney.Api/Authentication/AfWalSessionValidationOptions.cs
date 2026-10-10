using Microsoft.Extensions.Configuration;

namespace MobileMoney.Production.Authentication;

public sealed record AfWalSessionValidationOptions(
    Uri? IdentityBaseUri,
    TimeSpan Timeout)
{
    public const string BaseUrlConfigurationKey = "IdentityService:BaseUrl";
    public const string TimeoutConfigurationKey =
        "IdentityService:SessionValidationTimeoutSeconds";
    public const string BaseUrlEnvironmentVariable =
        "AFW_IDENTITY_SERVICE_BASE_URL";
    public const string TimeoutEnvironmentVariable =
        "AFW_IDENTITY_SESSION_VALIDATION_TIMEOUT_SECONDS";

    public static AfWalSessionValidationOptions FromConfiguration(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var baseUrl =
            configuration[BaseUrlConfigurationKey] ??
            Environment.GetEnvironmentVariable(BaseUrlEnvironmentVariable);

        Uri? baseUri = null;
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var parsed) &&
            (parsed.Scheme == Uri.UriSchemeHttps ||
             parsed.Scheme == Uri.UriSchemeHttp))
        {
            baseUri = parsed;
        }

        var timeoutValue =
            configuration[TimeoutConfigurationKey] ??
            Environment.GetEnvironmentVariable(TimeoutEnvironmentVariable);

        var timeoutSeconds =
            int.TryParse(timeoutValue, out var parsedSeconds) &&
            parsedSeconds is >= 1 and <= 30
                ? parsedSeconds
                : 3;

        return new(
            baseUri,
            TimeSpan.FromSeconds(timeoutSeconds));
    }
}
