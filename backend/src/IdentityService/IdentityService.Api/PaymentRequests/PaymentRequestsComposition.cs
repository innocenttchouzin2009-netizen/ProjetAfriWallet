using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Infrastructure;
using AfriWallet.PaymentRequests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IdentityService.Api.PaymentRequests;

public static class PaymentRequestsComposition
{
    public static IServiceCollection AddPaymentRequests(
        this IServiceCollection services,
        string connectionString,
        IConfiguration? configuration = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Payment requests connection string is required.", nameof(connectionString));
        }

        var workerOptions = PaymentRequestEventDispatchWorkerOptions.FromConfiguration(configuration);
        var deliveryOptions = workerOptions.ToDeliveryOptions();

        services.AddDbContext<PaymentRequestDbContext>(options => options.UseSqlite(connectionString));
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
        services.AddScoped<IPaymentRequestEventAttemptLedger, EfPaymentRequestEventAttemptLedger>();
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
        services.AddSingleton(deliveryOptions);
        services.AddSingleton<PaymentRequestEventOutboxWorkerState>();
        services.AddHostedService<PaymentRequestEventOutboxHostedWorker>();
        return services;
    }
}
