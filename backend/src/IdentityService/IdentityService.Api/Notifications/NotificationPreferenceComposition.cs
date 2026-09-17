using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;
using AfriWallet.Notifications.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Notifications;

public static class NotificationPreferenceComposition
{
    public static IServiceCollection AddNotificationPreferences(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Notification preferences database connection string is required.", nameof(connectionString));

        services.AddDbContext<NotificationPreferenceDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<INotificationPreferenceRepository, EfNotificationPreferenceRepository>();
        services.AddSingleton<INotificationChannelPolicyProvider, DefaultNotificationChannelPolicyProvider>();
        services.AddScoped<NotificationPreferenceApplicationService>();
        services.AddScoped<NotificationDeliveryRoutingService>();
        return services;
    }
}

public sealed class DefaultNotificationChannelPolicyProvider : INotificationChannelPolicyProvider
{
    private static readonly IReadOnlyList<NotificationChannelPolicy> Policies = [
        NotificationChannelPolicy.Create(NotificationChannel.InApp, defaultEnabled: true, userConfigurable: false),
        NotificationChannelPolicy.Create(NotificationChannel.Push, defaultEnabled: true, userConfigurable: true)
    ];

    public NotificationChannelPolicy Get(NotificationChannel channel)
    {
        if (!Enum.IsDefined(channel))
            throw new ArgumentOutOfRangeException(nameof(channel));

        return Policies.Single(policy => policy.Channel == channel);
    }

    public IReadOnlyList<NotificationChannelPolicy> List() => Policies;
}
