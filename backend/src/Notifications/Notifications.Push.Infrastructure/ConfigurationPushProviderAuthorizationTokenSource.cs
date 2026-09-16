using Microsoft.Extensions.Configuration;

namespace AfriWallet.Notifications.Push.Infrastructure;

public sealed class ConfigurationPushProviderAuthorizationTokenSource(IConfiguration configuration)
    : IPushProviderAuthorizationTokenSource
{
    public Task<string?> GetTokenAsync(
        PushProviderKind provider,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var token = provider switch
        {
            PushProviderKind.Fcm =>
                configuration["Notifications:PushProviders:Fcm:AccessToken"] ??
                Environment.GetEnvironmentVariable("AFW_FCM_ACCESS_TOKEN"),
            PushProviderKind.Apns =>
                configuration["Notifications:PushProviders:Apns:ProviderToken"] ??
                Environment.GetEnvironmentVariable("AFW_APNS_PROVIDER_TOKEN"),
            _ => throw new ArgumentOutOfRangeException(nameof(provider))
        };

        return Task.FromResult(string.IsNullOrWhiteSpace(token) ? null : token.Trim());
    }
}
