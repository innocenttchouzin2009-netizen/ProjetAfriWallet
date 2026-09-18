using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Persistence;
using AfriWallet.PaymentRequests.Webhooks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IdentityService.Api.PaymentRequests;

public static class PaymentRequestsComposition
{
    public static IServiceCollection AddPaymentRequests(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Payment requests connection string is required.", nameof(connectionString));
        }

        services.AddDbContext<PaymentRequestDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IPaymentRequestRepository, EfPaymentRequestRepository>();
        services.AddScoped<IPaymentRequestRecipientResolver, P2PRecipientResolver>();
        services.AddScoped<IPaymentRequestWalletOwnershipReader, WalletPaymentRequestOwnershipReader>();
        services.AddScoped<IPaymentRequestPaymentPort, P2PPaymentRequestPaymentPort>();
        services.AddScoped<IPaymentRequestOutboxStore, EfPaymentRequestOutboxStore>();
        services.AddScoped<PaymentRequestApplicationService>();
        services.AddScoped<PaymentRequestActionService>();
        return services;
    }

    public static IServiceCollection AddPaymentRequestWebhookDelivery(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection("PaymentRequests:Webhook");
        var enabled = section.GetValue<bool?>("Enabled") ?? false;
        if (!enabled)
        {
            return services;
        }

        var endpointValue = section["Endpoint"] ??
            Environment.GetEnvironmentVariable("AFW_PAYMENT_REQUEST_WEBHOOK_ENDPOINT");
        var signingSecret = section["SigningSecret"] ??
            Environment.GetEnvironmentVariable("AFW_PAYMENT_REQUEST_WEBHOOK_SIGNING_SECRET");

        if (!Uri.TryCreate(endpointValue, UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException("Payment request webhook endpoint is not configured as an absolute URI.");
        }

        if (string.IsNullOrWhiteSpace(signingSecret))
        {
            throw new InvalidOperationException("Payment request webhook signing secret is not configured.");
        }

        var options = new PaymentRequestWebhookTransportOptions(
            endpoint,
            section["SignatureHeaderName"] ?? "X-AfWal-Signature",
            section["IdempotencyHeaderName"] ?? "Idempotency-Key",
            section["EventTypeHeaderName"] ?? "X-AfWal-Event",
            section["MessageIdHeaderName"] ?? "X-AfWal-Message-Id");
        options.Validate();

        services.AddSingleton(options);
        services.AddSingleton<IPaymentRequestWebhookSigner>(
            new HmacSha256PaymentRequestWebhookSigner(signingSecret));
        services.AddHttpClient<IPaymentRequestOutboxTransport, HttpPaymentRequestOutboxTransport>();
        return services;
    }

    public static IServiceCollection AddPaymentRequestOutboxHosting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(PaymentRequestOutboxHostingOptions.SectionName);
        var options = new PaymentRequestOutboxHostingOptions
        {
            Enabled = section.GetValue<bool?>("Enabled") ?? false,
            IntervalSeconds = section.GetValue<int?>("IntervalSeconds") ?? 30,
            BatchSize = section.GetValue<int?>("BatchSize") ?? 50,
            MaxDeliveryAttempts = section.GetValue<int?>("MaxDeliveryAttempts") ?? 3
        };
        options.Validate();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(options);
        services.AddSingleton<PaymentRequestOutboxOperationalState>();
        services.AddSingleton<PaymentRequestOutboxDispatchCoordinator>();
        services.AddHostedService<PaymentRequestOutboxHostedService>();
        return services;
    }
}
