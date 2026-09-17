using System.Security.Claims;
using Reconciliation.Application.Review.Resolution;
using Reconciliation.Domain.ReviewResolution;

namespace Reconciliation.Api.Review;

public static class ReviewResolutionEndpoints
{
    public static IEndpointRouteBuilder MapReviewResolutionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/v1/reconciliation/reviews")
            .RequireAuthorization()
            .MapPost("/{reviewId:guid}/resolve", ResolveAsync);
        return endpoints;
    }

    private static async Task<IResult> ResolveAsync(
        Guid reviewId,
        ClaimsPrincipal principal,
        ReviewResolutionApplicationService service,
        CancellationToken cancellationToken)
    {
        var resolverId = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(resolverId))
            return Results.Unauthorized();

        try
        {
            var result = await service.ResolveAsync(
                new ResolveReviewCommand(reviewId, resolverId, DateTime.UtcNow),
                cancellationToken);

            return result.Status switch
            {
                ReviewResolutionStatus.Resolved => Results.Ok(ReviewResolutionResponse.From(result)),
                ReviewResolutionStatus.AlreadyResolved => Results.Ok(ReviewResolutionResponse.From(result)),
                ReviewResolutionStatus.NotEligible => Results.Conflict(new ReviewResolutionError(
                    ReviewResolutionErrorCode.NotEligible,
                    "Review must exist and be approved before it can be resolved.")),
                ReviewResolutionStatus.NoMatchingEvidence => Results.UnprocessableEntity(new ReviewResolutionError(
                    ReviewResolutionErrorCode.NoMatchingEvidence,
                    "No matching reconciliation evidence was found for the approved review.")),
                _ => Results.Conflict(new ReviewResolutionError(
                    ReviewResolutionErrorCode.Conflict,
                    "Review resolution could not be completed."))
            };
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new ReviewResolutionError(ReviewResolutionErrorCode.Validation, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new ReviewResolutionError(ReviewResolutionErrorCode.Conflict, ex.Message));
        }
    }
}
