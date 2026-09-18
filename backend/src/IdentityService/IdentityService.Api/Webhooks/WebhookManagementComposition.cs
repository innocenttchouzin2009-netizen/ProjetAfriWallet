using AfriWallet.Webhooks.Application;
using AfriWallet.Webhooks.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Webhooks;

public static class WebhookManagementComposition
{
    public static IServiceCollection AddWebhookManagement(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Webhook management connection string is required.", nameof(connectionString));

        services.AddDbContext<WebhookManagementDbContext>(
            options => options.UseSqlite(connectionString));
        services.AddScoped<IWebhookSubscriptionRepository, EfWebhookSubscriptionRepository>();
        services.AddScoped<WebhookManagementService>();
        return services;
    }
}
