using System.Security.Claims;
using AfriWallet.Reconciliation.Application;

namespace IdentityService.Api.Reconciliation;

public static class ReconciliationEndpoints
{
    public static IEndpointRouteBuilder MapReconciliationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/reconciliation/transfers")
            .RequireAuthorization();

        group.MapGet("/by-correlation/{correlationId:guid}", GetByCorrelationIdAsync);
        group.MapGet("/by-journal/{journalEntryId:guid}", GetByJournalEntryIdAsync);
        return endpoints;
    }

    private static Task<IResult> GetByCorrelationIdAsync(
        Guid correlationId,
        ClaimsPrincipal principal,
        WalletAwareTransferReconciliationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            principal,
            () => service.GetByCorrelationIdAsync(correlationId, cancellationToken),
            httpContext);

    private static Task<IResult> GetByJournalEntryIdAsync(
        Guid journalEntryId,
        ClaimsPrincipal principal,
        WalletAwareTransferReconciliationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            principal,
            () => service.GetByJournalEntryIdAsync(journalEntryId, cancellationToken),
            httpContext);

    private static async Task<IResult> ExecuteAsync(
        ClaimsPrincipal principal,
        Func<Task<WalletAwareTransferReconciliationResult>> lookup,
        HttpContext httpContext)
    {
        if (!Guid.TryParse(principal.FindFirst("sub")?.Value, out var userId))
        {
            return Results.Json(
                new ReconciliationErrorResponse(
                    ReconciliationErrorCode.Unauthorized,
                    "Authenticated user id is missing.",
                    httpContext.TraceIdentifier),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        try
        {
            var result = await lookup();
            if (result.Status is WalletAwareTransferReconciliationStatus.NotFound or
                WalletAwareTransferReconciliationStatus.NotTransferJournal ||
                result.Receipt is null)
            {
                return Results.NotFound(new ReconciliationErrorResponse(
                    ReconciliationErrorCode.NotFound,
                    "Transfer reconciliation record not found.",
                    httpContext.TraceIdentifier));
            }

            if (result.Status != WalletAwareTransferReconciliationStatus.Reconciled)
            {
                return Results.Conflict(new ReconciliationErrorResponse(
                    ReconciliationErrorCode.InvalidState,
                    result.Reason ?? "Transfer reconciliation data is inconsistent.",
                    httpContext.TraceIdentifier));
            }

            var receipt = result.Receipt;
            if (receipt.SourceWallet.OwnerId != userId && receipt.TargetWallet.OwnerId != userId)
            {
                return Results.NotFound(new ReconciliationErrorResponse(
                    ReconciliationErrorCode.NotFound,
                    "Transfer reconciliation record not found.",
                    httpContext.TraceIdentifier));
            }

            return Results.Ok(new ReconciliationTransferResponse(
                receipt.Transfer.TransferId,
                receipt.Transfer.JournalEntryId.Value,
                receipt.Transfer.CorrelationId,
                receipt.SourceWallet.WalletId,
                receipt.TargetWallet.WalletId,
                receipt.Transfer.CurrencyCode,
                receipt.Transfer.AmountMinor,
                receipt.Transfer.PostedAtUtc,
                receipt.SourceWallet.IsActive,
                receipt.TargetWallet.IsActive));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new ReconciliationErrorResponse(
                ReconciliationErrorCode.ValidationError,
                exception.Message,
                httpContext.TraceIdentifier));
        }
    }
}
