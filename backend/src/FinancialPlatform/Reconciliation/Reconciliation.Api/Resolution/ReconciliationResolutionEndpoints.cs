using System.Security.Claims;
using Reconciliation.Application.Resolution;
using Reconciliation.Domain.Resolutions;

namespace Reconciliation.Api.Resolution;

public static class ReconciliationResolutionEndpoints
{
    public static IEndpointRouteBuilder MapReconciliationResolutionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/reconciliation/reviews").RequireAuthorization();
        group.MapPost("/{reviewId:guid}/resolution", CreateAsync);
        group.MapGet("/{reviewId:guid}/resolution", GetAsync);
        group.MapGet("/{reviewId:guid}/resolution/audit", GetAuditAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        Guid reviewId,
        CreateReconciliationResolutionRequest request,
        ClaimsPrincipal principal,
        ReconciliationResolutionApplicationService service,
        CancellationToken cancellationToken)
    {
        var resolverId = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(resolverId))
            return Results.Unauthorized();

        if (!Enum.TryParse<ReconciliationResolutionDisposition>(request.Disposition, true, out var disposition) ||
            !Enum.IsDefined(disposition))
        {
            return Results.BadRequest(new ReconciliationResolutionError(
                "RECONCILIATION_RESOLUTION_VALIDATION",
                "Unknown reconciliation resolution disposition."));
        }

        try
        {
            var existing = await service.GetByReviewIdAsync(reviewId, cancellationToken);
            if (existing is not null)
            {
                if (IsEquivalent(existing, disposition, resolverId, request))
                    return Results.Ok(ReconciliationResolutionResponse.From(existing));

                return Results.Conflict(new ReconciliationResolutionError(
                    "RECONCILIATION_RESOLUTION_CONFLICT",
                    "Review already has a different reconciliation resolution."));
            }

            var result = await service.ResolveAsync(
                new ResolveReconciliationReviewCommand(
                    reviewId,
                    disposition,
                    resolverId,
                    request.Rationale,
                    request.EvidenceReference,
                    DateTime.UtcNow),
                cancellationToken);

            return result.Status switch
            {
                ReconciliationResolutionExecutionStatus.Created when result.Resolution is not null =>
                    Results.Created(
                        $"/api/v1/reconciliation/reviews/{reviewId}/resolution",
                        ReconciliationResolutionResponse.From(result.Resolution)),
                ReconciliationResolutionExecutionStatus.Existing when result.Resolution is not null =>
                    Results.Ok(ReconciliationResolutionResponse.From(result.Resolution)),
                ReconciliationResolutionExecutionStatus.ReviewNotFound =>
                    Results.NotFound(new ReconciliationResolutionError(
                        "RECONCILIATION_RESOLUTION_NOT_FOUND",
                        "Reconciliation review was not found.")),
                ReconciliationResolutionExecutionStatus.ReviewNotFinal =>
                    Results.Conflict(new ReconciliationResolutionError(
                        "RECONCILIATION_RESOLUTION_REVIEW_NOT_FINAL",
                        "Reconciliation review must be approved or rejected before resolution.")),
                _ => Results.Conflict(new ReconciliationResolutionError(
                    "RECONCILIATION_RESOLUTION_CONFLICT",
                    "Reconciliation resolution could not be created."))
            };
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new ReconciliationResolutionError(
                "RECONCILIATION_RESOLUTION_VALIDATION",
                ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new ReconciliationResolutionError(
                "RECONCILIATION_RESOLUTION_CONFLICT",
                ex.Message));
        }
    }

    private static async Task<IResult> GetAsync(
        Guid reviewId,
        ReconciliationResolutionApplicationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var resolution = await service.GetByReviewIdAsync(reviewId, cancellationToken);
            return resolution is null
                ? Results.NotFound()
                : Results.Ok(ReconciliationResolutionResponse.From(resolution));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new ReconciliationResolutionError(
                "RECONCILIATION_RESOLUTION_VALIDATION",
                ex.Message));
        }
    }

    private static async Task<IResult> GetAuditAsync(
        Guid reviewId,
        ReconciliationResolutionApplicationService service,
        IReconciliationResolutionAuditReader auditReader,
        CancellationToken cancellationToken)
    {
        try
        {
            var resolution = await service.GetByReviewIdAsync(reviewId, cancellationToken);
            if (resolution is null)
                return Results.NotFound();

            var audit = await auditReader.ListByReviewIdAsync(reviewId, cancellationToken);
            return Results.Ok(audit.Select(ReconciliationResolutionAuditResponse.From).ToArray());
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new ReconciliationResolutionError(
                "RECONCILIATION_RESOLUTION_VALIDATION",
                ex.Message));
        }
    }

    private static bool IsEquivalent(
        ReconciliationResolution existing,
        ReconciliationResolutionDisposition disposition,
        string resolverId,
        CreateReconciliationResolutionRequest request) =>
        existing.Disposition == disposition &&
        string.Equals(existing.ResolvedBy, resolverId.Trim(), StringComparison.Ordinal) &&
        string.Equals(existing.Rationale, request.Rationale?.Trim(), StringComparison.Ordinal) &&
        string.Equals(existing.EvidenceReference, request.EvidenceReference?.Trim(), StringComparison.Ordinal);
}
