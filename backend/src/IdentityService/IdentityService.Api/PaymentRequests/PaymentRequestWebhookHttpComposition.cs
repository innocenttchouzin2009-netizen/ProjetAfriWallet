using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Webhooks;

namespace IdentityService.Api.PaymentRequests;

public static class PaymentRequestWebhookHttpComposition
{
    private const string Prefix = "PaymentRequests:EventOutbox:Webhook";

    public static IServiceCollection AddPaymentRequestWebhookHttpDelivery(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var enabled = configuration.GetValue<bool?>($"{Prefix}:Enabled") ?? false;
        if (!enabled)
            return services;

        var endpointRaw = configuration[$"{Prefix}:Endpoint"];
        if (!Uri.TryCreate(endpointRaw, UriKind.Absolute, out var endpoint) ||
            (endpoint.Scheme != Uri.UriSchemeHttps && endpoint.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidOperationException(
                "Enabled payment request webhook delivery requires an absolute HTTP or HTTPS endpoint.");
        }

        var keyId = configuration[$"{Prefix}:KeyId"];
        var secret = configuration["AFW_PAYMENT_REQUEST_WEBHOOK_SECRET"] ??
                     Environment.GetEnvironmentVariable("AFW_PAYMENT_REQUEST_WEBHOOK_SECRET");

        if (string.IsNullOrWhiteSpace(keyId))
            throw new InvalidOperationException("Enabled payment request webhook delivery requires a key id.");
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException(
                "Enabled payment request webhook delivery requires AFW_PAYMENT_REQUEST_WEBHOOK_SECRET.");

        var nowUtc = DateTimeOffset.UtcNow;
        var secretProvider = new RotatingPaymentRequestWebhookSecretProvider(
            new PaymentRequestWebhookSecret(
                keyId,
                secret,
                DateTimeOffset.UnixEpoch));

        services.AddSingleton<IPaymentRequestWebhookSecretProvider>(secretProvider);
        services.AddSingleton<IPaymentRequestWebhookReplayGuard>(
            new InMemoryPaymentRequestWebhookReplayGuard());
        services.AddSingleton(PaymentRequestWebhookSecurityOptions.Default);
        services.AddSingleton(new PaymentRequestWebhookHttpDeliveryOptions(endpoint));
        services.AddSingleton<PaymentRequestWebhookSigner>();
        services.AddSingleton<PaymentRequestWebhookVerifier>();
        services.AddHttpClient<HttpPaymentRequestEventTransport>();
        services.AddScoped<IPaymentRequestEventTransport>(sp =>
            sp.GetRequiredService<HttpPaymentRequestEventTransport>());

        return services;
    }
}
