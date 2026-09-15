using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IdentityService.Api.PaymentRequests;

public static class PaymentRequestEventOutboxHostingExtensions
{
    public static IServiceCollection AddPaymentRequestEventOutboxHosting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = PaymentRequestEventOutboxHostingOptions.FromConfiguration(configuration);
        var deliveryOptions = new PaymentRequestEventDeliveryOptions(
            options.MaxAttempts,
            options.LeaseDuration,
            options.BaseRetryDelay);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(options);
        services.AddSingleton(deliveryOptions);
        services.AddSingleton<PaymentRequestEventOutboxDispatcherState>();
        services.AddScoped<IPaymentRequestEventOutboxStore, EfPaymentRequestEventOutboxStore>();
        services.AddScoped<PaymentRequestEventOutboxProcessor>();
        services.AddHostedService<PaymentRequestEventOutboxHostedService>();
        return services;
    }
}
