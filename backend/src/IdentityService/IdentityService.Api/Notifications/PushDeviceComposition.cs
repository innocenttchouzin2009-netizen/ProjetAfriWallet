using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Notifications;

public static class PushDeviceComposition
{
    public static IServiceCollection AddPushDeviceRegistration(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Push device database connection string is required.", nameof(connectionString));

        services.AddDbContext<PushDeviceRegistrationDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IPushDeviceRegistrationRepository, EfPushDeviceRegistrationRepository>();
        services.AddScoped<PushDeviceRegistrationService>();
        return services;
    }
}
