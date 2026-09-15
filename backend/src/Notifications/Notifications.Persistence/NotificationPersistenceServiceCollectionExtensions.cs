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
        services.AddScoped<INotificationDeliveryPort>(sp => sp.GetRequiredService<EfInAppNotificationStore>());
        services.AddScoped<IInAppNotificationReader>(sp => sp.GetRequiredService<EfInAppNotificationStore>());
        return services;
    }
}
