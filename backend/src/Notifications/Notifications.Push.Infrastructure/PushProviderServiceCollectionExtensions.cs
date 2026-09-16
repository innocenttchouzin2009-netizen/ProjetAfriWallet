using AfriWallet.Notifications.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AfriWallet.Notifications.Push.Infrastructure;

public static class PushProviderServiceCollectionExtensions
{
    public static IServiceCollection AddProviderBackedPushDelivery(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var useSandbox = bool.TryParse(
            configuration["Notifications:PushProviders:Apns:UseSandbox"] ??
            Environment.GetEnvironmentVariable("AFW_APNS_USE_SANDBOX"),
            out var sandbox) && sandbox;

        var options = new PushProviderOptions(
            configuration["Notifications:PushProviders:Fcm:ProjectId"] ??
                Environment.GetEnvironmentVariable("AFW_FCM_PROJECT_ID"),
            configuration["Notifications:PushProviders:Apns:Topic"] ??
                Environment.GetEnvironmentVariable("AFW_APNS_TOPIC"),
            useSandbox);

        services.AddSingleton(options);
        services.AddSingleton<IPushProviderAuthorizationTokenSource, ConfigurationPushProviderAuthorizationTokenSource>();
        services.AddHttpClient<FcmHttpV1PushProviderClient>();
        services.AddHttpClient<ApnsHttp2PushProviderClient>();
        services.AddTransient<IPushProviderClient>(provider => provider.GetRequiredService<FcmHttpV1PushProviderClient>());
        services.AddTransient<IPushProviderClient>(provider => provider.GetRequiredService<ApnsHttp2PushProviderClient>());
        services.AddScoped<IPushNotificationTransport, ProviderBackedPushNotificationTransport>();
        services.AddScoped<PushNotificationDeliveryService>();
        return services;
    }
}
