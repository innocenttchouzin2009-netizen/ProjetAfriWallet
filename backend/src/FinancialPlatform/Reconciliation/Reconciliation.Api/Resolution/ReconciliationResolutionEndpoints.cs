using System.Security.Claims;
using Reconciliation.Application.Resolution;
using Reconciliation.Domain.Resolutions;

namespace Reconciliation.Api.Resolution;

public static class ReconciliationResolutionEndpoints
{
    public static IEndpointRouteBuilder MapReconciliationResolutionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var reviewGroup = endpoints.MapGroup("/api/v1/reconciliation/reviews").RequireAuthorization();
        reviewGroup.MapPost("/{reviewId:guid}/resolution", CreateAsync);
        reviewGroup.MapGet("/{reviewId:guid}/resolution", GetAsync);
        reviewGroup.MapGet("/{reviewId:guid}/resolution/audit", GetAuditAsync);

        endpoints.MapGet("/api/v1/reconciliation/resolutions", ListAsync)
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        Guid reviewId,
        CreateReconciliationResolutionRequest request,
        ClaimsPrincipal principal,
        ReconciliationResolutionApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!ReconciliationResolutionAccessScopeFactory.TryCreate(principal, out var accessScope) || accessScope is null)
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
            var result = await service.ResolveAsync(
                new ResolveReconciliationReviewCommand(
                    reviewId,
                    disposition,
                    accessScope.ActorId,
                    request.Rationale,
                    request.EvidenceReference,
                    DateTime.UtcNow),
                accessScope,
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
                ReconciliationResolutionExecutionStatus.AccessDenied =>
                    Results.Json(
                        new ReconciliationResolutionError(
                            "RECONCILIATION_RESOLUTION_FORBIDDEN",
                            "Authenticated actor is not authorized for this reconciliation partner."),
                        statusCode: StatusCodes.Status403Forbidden),
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
        ClaimsPrincipal principal,
        ReconciliationResolutionApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!ReconciliationResolutionAccessScopeFactory.TryCreate(principal, out var accessScope) || accessScope is null)
            return Results.Unauthorized();

        try
        {
            var result = await service.GetByReviewIdAsync(reviewId, accessScope, cancellationToken);
            return result.Status switch
            {
                ReconciliationResolutionLookupStatus.Found when result.Resolution is not null =>
                    Results.Ok(ReconciliationResolutionResponse.From(result.Resolution)),
                ReconciliationResolutionLookupStatus.NotFound => Results.NotFound(),
                ReconciliationResolutionLookupStatus.AccessDenied => Results.Json(
                    new ReconciliationResolutionError(
                        "RECONCILIATION_RESOLUTION_FORBIDDEN",
                        "Authenticated actor is not authorized for this reconciliation partner."),
                    statusCode: StatusCodes.Status403Forbidden),
                _ => Results.NotFound()
            };
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
        ClaimsPrincipal principal,
        ReconciliationResolutionApplicationService service,
        IReconciliationResolutionAuditReader auditReader,
        CancellationToken cancellationToken)
    {
        if (!ReconciliationResolutionAccessScopeFactory.TryCreate(principal, out var accessScope) || accessScope is null)
            return Results.Unauthorized();

        try
        {
            var lookup = await service.GetByReviewIdAsync(reviewId, accessScope, cancellationToken);
            if (lookup.Status == ReconciliationResolutionLookupStatus.NotFound)
                return Results.NotFound();
            if (lookup.Status == ReconciliationResolutionLookupStatus.AccessDenied)
            {
                return Results.Json(
                    new ReconciliationResolutionError(
                        "RECONCILIATION_RESOLUTION_FORBIDDEN",
                        "Authenticated actor is not authorized for this reconciliation partner."),
                    statusCode: StatusCodes.Status403Forbidden);
            }

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

    private static async Task<IResult> ListAsync(
        string? partnerId,
        string? disposition,
        string? resolvedBy,
        DateTime? resolvedFromUtc,
        DateTime? resolvedToUtc,
        int? limit,
        ClaimsPrincipal principal,
        ReconciliationResolutionApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!ReconciliationResolutionAccessScopeFactory.TryCreate(principal, out var accessScope) || accessScope is null)
            return Results.Unauthorized();

        ReconciliationResolutionDisposition? parsedDisposition = null;
        if (!string.IsNullOrWhiteSpace(disposition))
        {
            if (!Enum.TryParse<ReconciliationResolutionDisposition>(disposition, true, out var value) ||
                !Enum.IsDefined(value))
            {
                return Results.BadRequest(new ReconciliationResolutionError(
                    "RECONCILIATION_RESOLUTION_VALIDATION",
                    "Unknown reconciliation resolution disposition."));
            }
            parsedDisposition = value;
        }

        try
        {
            var result = await service.ListAsync(
                new ReconciliationResolutionQuery(
                    partnerId,
                    parsedDisposition,
                    resolvedBy,
                    NormalizeUtc(resolvedFromUtc),
                    NormalizeUtc(resolvedToUtc),
                    limit ?? 100),
                accessScope,
                cancellationToken);

            return result.Status == ReconciliationResolutionListStatus.AccessDenied
                ? Results.Json(
                    new ReconciliationResolutionError(
                        "RECONCILIATION_RESOLUTION_FORBIDDEN",
                        "Authenticated actor is not authorized for the requested reconciliation partner."),
                    statusCode: StatusCodes.Status403Forbidden)
                : Results.Ok(result.Resolutions.Select(ReconciliationResolutionResponse.From).ToArray());
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new ReconciliationResolutionError(
                "RECONCILIATION_RESOLUTION_VALIDATION",
                ex.Message));
        }
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (value is null)
            return null;

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
            _ => value.Value.ToUniversalTime()
        };
    }
}
