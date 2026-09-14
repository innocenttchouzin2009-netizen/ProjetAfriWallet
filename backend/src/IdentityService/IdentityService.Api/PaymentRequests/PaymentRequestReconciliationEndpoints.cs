using System.Security.Claims;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Application;

namespace IdentityService.Api.PaymentRequests;

public static class PaymentRequestReconciliationEndpoints
{
    public static IEndpointRouteBuilder MapPaymentRequestReconciliationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/v1/payment-requests")
            .RequireAuthorization()
            .MapPost("/{id:guid}/reconcile", ReconcileAsync);
        return endpoints;
    }

    private static async Task<IResult> ReconcileAsync(
        Guid id,
        ClaimsPrincipal principal,
        IPaymentRequestRepository repository,
        IWalletRepository walletRepository,
        PaymentRequestReconciliationService reconciliationService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirst("sub")?.Value, out var userId))
        {
            return Results.Json(
                new PaymentRequestErrorResponse(
                    PaymentRequestErrorCode.Unauthorized,
                    "Authenticated user id is missing.",
                    httpContext.TraceIdentifier),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (id == Guid.Empty)
        {
            return Results.BadRequest(new PaymentRequestErrorResponse(
                PaymentRequestErrorCode.ValidationError,
                "Payment request id is required.",
                httpContext.TraceIdentifier));
        }

        var requestId = PaymentRequestId.From(id);
        var request = await repository.GetAsync(requestId, cancellationToken);
        if (request is null)
        {
            return NotFound(httpContext);
        }

        var requesterWallet = await walletRepository.GetAsync(request.RequesterWalletId, cancellationToken);
        if (requesterWallet is null || requesterWallet.OwnerId != userId)
        {
            return NotFound(httpContext);
        }

        try
        {
            var result = await reconciliationService.ReconcileAsync(requestId, cancellationToken);
            return result.Status switch
            {
                PaymentRequestReconciliationStatus.Reconciled when result.Request is not null =>
                    Results.Ok(ToResponse(result.Status, result.Request)),
                PaymentRequestReconciliationStatus.AlreadyPaid when result.Request is not null =>
                    Results.Ok(ToResponse(result.Status, result.Request)),
                PaymentRequestReconciliationStatus.RequestNotFound => NotFound(httpContext),
                PaymentRequestReconciliationStatus.TransferNotFound => Conflict(
                    httpContext,
                    "No certified transfer receipt was found for this payment request."),
                PaymentRequestReconciliationStatus.NotEligible => Conflict(
                    httpContext,
                    "Payment request is not eligible for reconciliation."),
                _ => Conflict(httpContext, "Payment request could not be reconciled.")
            };
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new PaymentRequestErrorResponse(
                PaymentRequestErrorCode.ValidationError,
                exception.Message,
                httpContext.TraceIdentifier));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(httpContext, exception.Message);
        }
    }

    private static IResult NotFound(HttpContext httpContext) => Results.NotFound(
        new PaymentRequestErrorResponse(
            PaymentRequestErrorCode.NotFound,
            "Payment request not found.",
            httpContext.TraceIdentifier));

    private static IResult Conflict(HttpContext httpContext, string message) => Results.Conflict(
        new PaymentRequestErrorResponse(
            PaymentRequestErrorCode.Conflict,
            message,
            httpContext.TraceIdentifier));

    private static PaymentRequestReconciliationHttpResponse ToResponse(
        PaymentRequestReconciliationStatus reconciliationStatus,
        PaymentRequestSnapshot snapshot) => new(
            snapshot.Id.Value,
            snapshot.Status.ToString(),
            reconciliationStatus.ToString(),
            snapshot.TransferId,
            snapshot.ClosedAtUtc);
}

public sealed record PaymentRequestReconciliationHttpResponse(
    Guid Id,
    string Status,
    string ReconciliationStatus,
    Guid? TransferId,
    DateTimeOffset? ClosedAtUtc);
