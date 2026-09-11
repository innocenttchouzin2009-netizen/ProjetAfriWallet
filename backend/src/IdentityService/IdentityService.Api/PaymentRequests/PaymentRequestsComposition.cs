using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Persistence;
using Microsoft.EntityFrameworkCore;

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
        services.AddScoped<PaymentRequestApplicationService>();
        services.AddScoped<PaymentRequestActionService>();
        return services;
    }
}
