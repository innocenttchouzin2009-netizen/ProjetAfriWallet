using AfriWallet.P2P.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AfriWallet.P2P.Directory.Persistence;

public static class RecipientDirectoryServiceCollectionExtensions
{
    public static IServiceCollection AddAuthoritativeP2PRecipientDirectory(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Recipient directory connection string is required.", nameof(connectionString));
        }

        services.AddDbContext<RecipientDirectoryDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IAfWalIdentityDirectory, EfAfWalIdentityDirectory>();
        services.AddScoped<IQrRecipientDirectory, EfQrRecipientDirectory>();
        return services;
    }
}
