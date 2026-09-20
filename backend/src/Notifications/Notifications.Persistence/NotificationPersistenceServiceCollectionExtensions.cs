using AfriWallet.Notifications.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AfriWallet.Notifications.Persistence;

public static class NotificationPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddInAppNotificationPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Notification database connection string is required.", nameof(connectionString));
        }

        services.AddDbContext<NotificationDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<EfInAppNotificationStore>();
        services.AddScoped<IInAppNotificationReader>(sp => sp.GetRequiredService<EfInAppNotificationStore>());

        services.AddScoped<INotificationDeliveryRepository, EfNotificationDeliveryRepository>();
        services.AddScoped<INotificationDispatchPort, InAppNotificationDispatchPort>();
        services.AddScoped<NotificationDeliveryFanoutService>();
        services.AddScoped<INotificationDeliveryPort, PersistentNotificationDeliveryService>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
