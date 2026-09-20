using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reconciliation.Application.Resolution;

namespace Reconciliation.Infrastructure.Resolutions;

public static class ReconciliationResolutionPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddReconciliationResolutionPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Reconciliation resolution database connection string is required.", nameof(connectionString));

        services.AddDbContext<ReconciliationResolutionDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<EfReconciliationResolutionRepository>();
        services.AddScoped<IReconciliationResolutionRepository>(services =>
            services.GetRequiredService<EfReconciliationResolutionRepository>());
        services.AddScoped<IReconciliationResolutionAuditReader>(services =>
            services.GetRequiredService<EfReconciliationResolutionRepository>());
        return services;
    }
}
