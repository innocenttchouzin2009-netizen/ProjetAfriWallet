using AfriWallet.PushNotifications.Application;
using AfriWallet.PushNotifications.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.PushNotifications;

public static class PushDeviceComposition
{
    public static IServiceCollection AddPushDeviceRegistration(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Push notifications connection string is required.", nameof(connectionString));
        services.AddDbContext<PushNotificationDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IPushDeviceRepository, EfPushDeviceRepository>();
        services.AddScoped<PushDeviceRegistrationService>();
        return services;
    }
}
