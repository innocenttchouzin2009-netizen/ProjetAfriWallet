using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Infrastructure;
using AfriWallet.PaymentRequests.Persistence;
using AfriWallet.PaymentRequests.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IdentityService.Api.PaymentRequests;

public static class PaymentRequestsComposition
{
    private const string WebhookConfigurationPrefix =
        "PaymentRequests:EventOutbox:Webhook";

    public static IServiceCollection AddPaymentRequests(
        this IServiceCollection services,
        string connectionString,
        IConfiguration? configuration = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "Payment requests connection string is required.",
                nameof(connectionString));
        }

        var workerOptions =
            PaymentRequestEventDispatchWorkerOptions.FromConfiguration(configuration);
        var recoveryWorkerOptions =
            PaymentRequestEventRecoveryWorkerOptions.FromConfiguration(configuration);
        var deliveryOptions = workerOptions.ToDeliveryOptions();

        services.AddDbContext<PaymentRequestDbContext>(
            options => options.UseSqlite(connectionString));
        services.AddScoped<IPaymentRequestRepository, EfPaymentRequestRepository>();
        services.AddScoped<IPaymentRequestQueryRepository, EfPaymentRequestRepository>();
        services.AddScoped<IPaymentRequestLifecycleMutationStore, EfPaymentRequestLifecycleMutationStore>();
        services.AddScoped<IPaymentRequestRecipientResolver, P2PRecipientResolver>();
        services.AddScoped<IPaymentRequestWalletOwnershipReader, WalletPaymentRequestOwnershipReader>();
        services.AddScoped<IPaymentRequestPaymentPort, P2PPaymentRequestPaymentPort>();
        services.AddScoped<IPaymentRequestOwnedWalletReader, WalletRegistryOwnedWalletReader>();
        services.AddScoped<IPaymentRequestOwnedRecipientReferenceReader, AuthoritativeRecipientReferenceReader>();
        services.AddScoped<IPaymentRequestReconciliationPort, TransferCorrelationPaymentRequestReconciliationPort>();
        services.AddScoped<IPaymentRequestEventOutboxStore, EfPaymentRequestEventOutboxStore>();
        services.AddScoped<IPaymentRequestEventRecoveryStore, EfPaymentRequestEventRecoveryStore>();
        services.AddScoped<IPaymentRequestEventAttemptLedger, EfPaymentRequestEventAttemptLedger>();
        services.AddScoped<IPaymentRequestEventAttemptFinalizer, EfPaymentRequestEventAttemptFinalizer>();
        services.AddScoped<IPaymentRequestEventOutboxDiagnostics, EfPaymentRequestEventOutboxDiagnostics>();
        services.AddScoped<IPaymentRequestEventDeliveryPort, ProviderNeutralPaymentRequestEventDeliveryAdapter>();
        services.AddScoped<PaymentRequestEventOutboxProcessor>();
        services.AddScoped<PaymentRequestEventOutboxOperationalHealthService>();
        services.AddScoped<PaymentRequestApplicationService>();
        services.AddScoped<PaymentRequestActionService>();
        services.AddScoped<AuthorizedPaymentRequestQueryService>();
        services.AddScoped<PaymentRequestRecoveryService>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(workerOptions);
        services.AddSingleton(recoveryWorkerOptions);
        services.AddSingleton(deliveryOptions);
        services.AddSingleton<PaymentRequestEventOutboxWorkerState>();

        AddWebhookTransportIfConfigured(services, configuration);

        services.AddHostedService<PaymentRequestEventOutboxRecoveryHostedWorker>();
        services.AddHostedService<PaymentRequestEventOutboxHostedWorker>();
        return services;
    }

    private static void AddWebhookTransportIfConfigured(
        IServiceCollection services,
        IConfiguration? configuration)
    {
        if (configuration is null)
            return;

        var endpointRaw = configuration[$"{WebhookConfigurationPrefix}:Endpoint"];
        var keyId = configuration[$"{WebhookConfigurationPrefix}:KeyId"];
        var secret = configuration[$"{WebhookConfigurationPrefix}:Secret"];

        var configuredCount = new[] { endpointRaw, keyId, secret }
            .Count(value => !string.IsNullOrWhiteSpace(value));

        if (configuredCount == 0)
            return;

        if (configuredCount != 3)
        {
            throw new InvalidOperationException(
                "Payment request webhook transport requires Endpoint, KeyId and Secret together.");
        }

        if (!Uri.TryCreate(endpointRaw, UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException(
                "Payment request webhook endpoint is invalid.");
        }

        var httpOptions = new PaymentRequestWebhookHttpDeliveryOptions(endpoint);
        httpOptions.Validate();

        var secretProvider = new RotatingPaymentRequestWebhookSecretProvider(
            new PaymentRequestWebhookSecret(
                keyId!,
                secret!,
                DateTimeOffset.UnixEpoch));

        services.AddSingleton<IPaymentRequestWebhookSecretProvider>(secretProvider);
        services.AddSingleton<PaymentRequestWebhookSigner>();
        services.AddSingleton(httpOptions);
        services.AddHttpClient<IPaymentRequestEventTransport, HttpPaymentRequestEventTransport>();
    }
}
