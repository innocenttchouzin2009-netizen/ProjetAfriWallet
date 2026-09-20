using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reconciliation.Application.Review;

namespace Reconciliation.Infrastructure.Repositories;

public static class ReconciliationReviewPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddReconciliationReviewPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Reconciliation review database connection string is required.", nameof(connectionString));

        services.AddDbContext<ReconciliationReviewDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IReconciliationReviewRepository, EfReconciliationReviewRepository>();
        return services;
    }
}
