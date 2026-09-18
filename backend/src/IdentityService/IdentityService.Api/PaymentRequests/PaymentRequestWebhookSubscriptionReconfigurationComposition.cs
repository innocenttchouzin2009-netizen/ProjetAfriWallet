using AfriWallet.PaymentRequests.Webhooks;
using AfriWallet.PaymentRequests.WebhookSubscriptions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.PaymentRequests;

public static class PaymentRequestWebhookSubscriptionReconfigurationComposition
{
    public static IServiceCollection AddPaymentRequestWebhookSubscriptionReconfiguration(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Webhook subscriptions connection string is required.", nameof(connectionString));

        services.AddDbContext<PaymentRequestWebhookSubscriptionDbContext>(
            options => options.UseSqlite(connectionString));
        services.AddScoped<IPaymentRequestWebhookSubscriptionRegistry, EfPaymentRequestWebhookSubscriptionRegistry>();
        return services;
    }
}
