using Microsoft.Extensions.DependencyInjection;
using Reconciliation.Application.Review.Resolution;
using Reconciliation.Infrastructure.Repositories;
using Reconciliation.Infrastructure.ReviewResolution;

namespace Reconciliation.Api.Review;

public static class ReviewResolutionComposition
{
    public static IServiceCollection AddReviewResolutionModule(this IServiceCollection services)
    {
        services.AddScoped<IReviewResolutionReviewReader, DurableReviewResolutionReviewReader>();
        services.AddScoped<IReviewResolutionEvidenceMatcher, ReconciliationDataReviewResolutionEvidenceMatcher>();
        services.AddScoped<IReviewResolutionStore, EfReviewResolutionStore>();
        services.AddScoped<ReviewResolutionApplicationService>();
        return services;
    }
}
