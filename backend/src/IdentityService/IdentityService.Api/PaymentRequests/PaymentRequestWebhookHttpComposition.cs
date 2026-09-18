using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Webhooks;
using AfriWallet.PaymentRequests.WebhookSubscriptions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.PaymentRequests;

public static class PaymentRequestWebhookHttpComposition
{
    private const string Prefix = "PaymentRequests:EventOutbox:Webhook";

    public static IServiceCollection AddPaymentRequestWebhookHttpDelivery(
        this IServiceCollection services,
        IConfiguration configuration,
        string subscriptionsConnectionString)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (string.IsNullOrWhiteSpace(subscriptionsConnectionString))
            throw new ArgumentException("Webhook subscriptions connection string is required.", nameof(subscriptionsConnectionString));

        services.AddDbContext<PaymentRequestWebhookSubscriptionDbContext>(
            options => options.UseSqlite(subscriptionsConnectionString));
        services.AddScoped<IPaymentRequestWebhookSubscriptionRegistry, EfPaymentRequestWebhookSubscriptionRegistry>();
        services.AddScoped<IPaymentRequestWebhookSubscriptionAuditStore, EfPaymentRequestWebhookSubscriptionAuditStore>();
        services.AddScoped<IPaymentRequestWebhookDeliveryAttemptStore, EfPaymentRequestWebhookDeliveryAttemptStore>();
        services.AddScoped<IPaymentRequestWebhookSigningSecretResolver, EnvironmentPaymentRequestWebhookSigningSecretResolver>();
        services.AddHttpClient<HttpPaymentRequestWebhookConnectivityProbe>();
        services.AddScoped<IPaymentRequestWebhookConnectivityProbe>(sp =>
            sp.GetRequiredService<HttpPaymentRequestWebhookConnectivityProbe>());

        var enabled = configuration.GetValue<bool?>($"{Prefix}:Enabled") ?? false;
        if (!enabled)
            return services;

        var receiverKeyId = configuration[$"{Prefix}:ReferenceReceiverKeyId"];
        var receiverSecret = configuration["AFW_PAYMENT_REQUEST_WEBHOOK_RECEIVER_SECRET"] ??
                             Environment.GetEnvironmentVariable("AFW_PAYMENT_REQUEST_WEBHOOK_RECEIVER_SECRET") ??
                             configuration["AFW_PAYMENT_REQUEST_WEBHOOK_SECRET"] ??
                             Environment.GetEnvironmentVariable("AFW_PAYMENT_REQUEST_WEBHOOK_SECRET");

        if (string.IsNullOrWhiteSpace(receiverKeyId))
            throw new InvalidOperationException("Enabled payment request webhook receiver requires ReferenceReceiverKeyId.");
        if (string.IsNullOrWhiteSpace(receiverSecret))
            throw new InvalidOperationException("Enabled payment request webhook receiver requires a receiver signing secret.");

        var receiverSecretProvider = new RotatingPaymentRequestWebhookSecretProvider(
            new PaymentRequestWebhookSecret(
                receiverKeyId,
                receiverSecret,
                DateTimeOffset.UnixEpoch));

        services.AddSingleton<IPaymentRequestWebhookSecretProvider>(receiverSecretProvider);
        services.AddSingleton<IPaymentRequestWebhookReplayGuard>(new InMemoryPaymentRequestWebhookReplayGuard());
        services.AddSingleton(PaymentRequestWebhookSecurityOptions.Default);
        services.AddSingleton<PaymentRequestWebhookVerifier>();

        services.AddHttpClient<RegistryBackedHttpPaymentRequestEventTransport>();
        services.AddScoped<IPaymentRequestEventTransport>(sp =>
            sp.GetRequiredService<RegistryBackedHttpPaymentRequestEventTransport>());

        return services;
    }
}
