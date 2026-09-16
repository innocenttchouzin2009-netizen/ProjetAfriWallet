using System.Security.Cryptography;
using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Notifications;

public static class DevicePushComposition
{
    public static IServiceCollection AddDevicePushRegistration(
        this IServiceCollection services,
        string connectionString,
        string protectionKeyBase64)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Push device database connection string is required.", nameof(connectionString));
        if (string.IsNullOrWhiteSpace(protectionKeyBase64))
            throw new ArgumentException("Push token protection key is required.", nameof(protectionKeyBase64));

        byte[] key;
        try
        {
            key = Convert.FromBase64String(protectionKeyBase64);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("Push token protection key must be valid Base64.", nameof(protectionKeyBase64), exception);
        }

        if (key.Length != 32)
        {
            CryptographicOperations.ZeroMemory(key);
            throw new ArgumentException("Push token protection key must decode to exactly 32 bytes.", nameof(protectionKeyBase64));
        }

        var protector = new AesGcmPushTokenProtector(key);
        CryptographicOperations.ZeroMemory(key);

        services.AddDbContext<NotificationInboxDbContext>(options => options.UseSqlite(connectionString));
        services.AddSingleton<IPushTokenProtector>(protector);
        services.AddScoped<IDevicePushRegistrationRepository, EfDevicePushRegistrationRepository>();
        services.AddScoped<DevicePushRegistrationService>();
        return services;
    }
}
