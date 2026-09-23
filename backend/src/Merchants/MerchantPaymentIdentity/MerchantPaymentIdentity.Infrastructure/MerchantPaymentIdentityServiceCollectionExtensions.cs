using AfriWallet.Merchants.PaymentIdentity.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AfriWallet.Merchants.PaymentIdentity.Infrastructure;

public static class MerchantPaymentIdentityServiceCollectionExtensions
{
    public static IServiceCollection AddMerchantPaymentIdentity(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Merchant payment identity database connection string is required.", nameof(connectionString));

        services.AddDbContext<MerchantPaymentIdentityDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IMerchantPaymentIdentityRegistry, EfMerchantPaymentIdentityRegistry>();
        services.AddScoped<IMerchantPaymentIdentityAuditStore, EfMerchantPaymentIdentityAuditStore>();
        services.AddScoped<MerchantPaymentIdentityService>();
        services.AddScoped<IMerchantPaymentIdentityResolver>(sp => sp.GetRequiredService<MerchantPaymentIdentityService>());
        return services;
    }
}
