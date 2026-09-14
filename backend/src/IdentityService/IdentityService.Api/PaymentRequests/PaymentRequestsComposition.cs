using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Persistence;
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
