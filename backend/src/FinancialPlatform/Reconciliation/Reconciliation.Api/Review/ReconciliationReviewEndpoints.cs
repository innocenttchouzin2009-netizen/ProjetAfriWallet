using System.Security.Claims;
using Reconciliation.Application.Review;

namespace Reconciliation.Api.Review;

public static class ReconciliationReviewEndpoints
{
    public static IEndpointRouteBuilder MapReconciliationReviewEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/reconciliation/reviews").RequireAuthorization();
        group.MapGet("/", ListAsync);
        group.MapGet("/{reviewId:guid}", GetAsync);
        group.MapPost("/{reviewId:guid}/decision", DecideAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        string partnerId,
        string? status,
        ReconciliationReviewApplicationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            ReconciliationReviewStatus? parsedStatus = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<ReconciliationReviewStatus>(status, true, out var value))
                    return Results.BadRequest(new ReconciliationReviewError("RECONCILIATION_REVIEW_VALIDATION", "Unknown review status."));
                parsedStatus = value;
            }

            var items = await service.ListAsync(partnerId, parsedStatus, cancellationToken);
            return Results.Ok(items.Select(ReconciliationReviewResponse.From).ToArray());
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new ReconciliationReviewError("RECONCILIATION_REVIEW_VALIDATION", ex.Message));
        }
    }

    private static async Task<IResult> GetAsync(
        Guid reviewId,
        ReconciliationReviewApplicationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await service.GetAsync(reviewId, cancellationToken);
            return item is null ? Results.NotFound() : Results.Ok(ReconciliationReviewResponse.From(item));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new ReconciliationReviewError("RECONCILIATION_REVIEW_VALIDATION", ex.Message));
        }
    }

    private static async Task<IResult> DecideAsync(
        Guid reviewId,
        ReconciliationReviewDecisionRequest request,
        ClaimsPrincipal principal,
        ReconciliationReviewApplicationService service,
        CancellationToken cancellationToken)
    {
        var reviewerId = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(reviewerId))
            return Results.Unauthorized();

        if (!Enum.TryParse<ReconciliationReviewStatus>(request.Decision, true, out var decision) ||
            decision == ReconciliationReviewStatus.PendingReview)
            return Results.BadRequest(new ReconciliationReviewError("RECONCILIATION_REVIEW_VALIDATION", "Decision must be Approved, Rejected or Escalated."));

        try
        {
            var item = await service.DecideAsync(
                new ReconciliationReviewDecisionCommand(
                    reviewId,
                    decision,
                    reviewerId,
                    request.Reason,
                    DateTime.UtcNow),
                cancellationToken);

            return item is null ? Results.NotFound() : Results.Ok(ReconciliationReviewResponse.From(item));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new ReconciliationReviewError("RECONCILIATION_REVIEW_VALIDATION", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new ReconciliationReviewError("RECONCILIATION_REVIEW_CONFLICT", ex.Message));
        }
    }
}
