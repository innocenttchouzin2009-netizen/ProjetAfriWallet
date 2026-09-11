using System.Security.Claims;
using AfriWallet.P2P.Domain;
using AfriWallet.PaymentRequests.Application;
using AfriWallet.PaymentRequests.Domain;
using AfriWallet.Wallet.Application;
using AfriWallet.Wallet.Domain;

namespace IdentityService.Api.PaymentRequests;

public static class PaymentRequestEndpoints
{
    public static IEndpointRouteBuilder MapPaymentRequestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/payment-requests").RequireAuthorization();
        group.MapPost("/", CreateAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/{id:guid}/decline", DeclineAsync);
        group.MapPost("/{id:guid}/cancel", CancelAsync);
        group.MapPost("/{id:guid}/accept", AcceptAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreatePaymentRequestHttpRequest request,
        ClaimsPrincipal principal,
        IWalletRepository walletRepository,
        PaymentRequestApplicationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return Unauthorized(httpContext);
        }

        if (request.RequesterWalletId == Guid.Empty ||
            request.CorrelationId == Guid.Empty ||
            request.AmountMinor <= 0 ||
            string.IsNullOrWhiteSpace(request.RecipientKind) ||
            string.IsNullOrWhiteSpace(request.RecipientValue) ||
            string.IsNullOrWhiteSpace(request.CurrencyCode))
        {
            return Results.BadRequest(new PaymentRequestErrorResponse(
                PaymentRequestErrorCode.ValidationError,
                "Requester wallet, recipient, currency, positive amount and correlation id are required.",
                httpContext.TraceIdentifier));
        }

        var requesterWallet = await walletRepository.GetAsync(WalletId.From(request.RequesterWalletId), cancellationToken);
        if (requesterWallet is null || requesterWallet.OwnerId != userId)
        {
            return NotFound(httpContext, "Requester wallet not found.");
        }

        try
        {
            var currency = Currency.Create(request.CurrencyCode);
            if (requesterWallet.Currency != currency)
            {
                throw new ArgumentException("Requester wallet currency must match the payment request currency.", nameof(request.CurrencyCode));
            }

            var recipient = CreateRecipientReference(request.RecipientKind, request.RecipientValue);
            var result = await service.CreateAsync(
                new CreatePaymentRequestCommand(
                    requesterWallet.Id,
                    recipient,
                    currency,
                    request.AmountMinor,
                    request.CorrelationId,
                    DateTimeOffset.UtcNow,
                    request.ExpiresAtUtc),
                cancellationToken);

            return result.Status switch
            {
                CreatePaymentRequestStatus.Created when result.Request is not null =>
                    Results.Created($"/api/v1/payment-requests/{result.Request.Id.Value}", ToResponse(result.Request)),
                CreatePaymentRequestStatus.Existing when result.Request is not null =>
                    Results.Ok(ToResponse(result.Request)),
                CreatePaymentRequestStatus.RecipientNotFound =>
                    Results.NotFound(new PaymentRequestErrorResponse(
                        PaymentRequestErrorCode.RecipientNotFound,
                        "Recipient was not found for the requested currency.",
                        httpContext.TraceIdentifier)),
                CreatePaymentRequestStatus.SelfRequestNotAllowed =>
                    Conflict(httpContext, "Requester and payer wallets must be different."),
                _ => Conflict(httpContext, "Payment request could not be created.")
            };
        }
        catch (ArgumentException exception)
        {
            return ValidationError(httpContext, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(httpContext, exception.Message);
        }
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ClaimsPrincipal principal,
        IWalletRepository walletRepository,
        PaymentRequestApplicationService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return Unauthorized(httpContext);
        }

        if (id == Guid.Empty)
        {
            return ValidationError(httpContext, "Payment request id is required.");
        }

        var snapshot = await service.GetAsync(PaymentRequestId.From(id), cancellationToken);
        if (snapshot is null)
        {
            return NotFound(httpContext, "Payment request not found.");
        }

        var requesterWallet = await walletRepository.GetAsync(snapshot.RequesterWalletId, cancellationToken);
        if (requesterWallet is null || requesterWallet.OwnerId != userId)
        {
            return NotFound(httpContext, "Payment request not found.");
        }

        return Results.Ok(ToResponse(snapshot));
    }

    private static Task<IResult> DeclineAsync(
        Guid id,
        ClaimsPrincipal principal,
        PaymentRequestActionService service,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        ExecuteActionAsync(id, principal, service.DeclineAsync, httpContext, cancellationToken);

    private static Task<IResult> CancelAsync(
        Guid id,
        ClaimsPrincipal principal,
        PaymentRequestActionService service,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        ExecuteActionAsync(id, principal, service.CancelAsync, httpContext, cancellationToken);

    private static Task<IResult> AcceptAsync(
        Guid id,
        ClaimsPrincipal principal,
        PaymentRequestActionService service,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        ExecuteActionAsync(id, principal, service.AcceptAndPayAsync, httpContext, cancellationToken);

    private static async Task<IResult> ExecuteActionAsync(
        Guid id,
        ClaimsPrincipal principal,
        Func<PaymentRequestId, Guid, DateTimeOffset, CancellationToken, Task<PaymentRequestActionResult>> action,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return Unauthorized(httpContext);
        }

        if (id == Guid.Empty)
        {
            return ValidationError(httpContext, "Payment request id is required.");
        }

        try
        {
            var result = await action(PaymentRequestId.From(id), userId, DateTimeOffset.UtcNow, cancellationToken);
            return result.Status switch
            {
                PaymentRequestActionStatus.Success when result.Request is not null => Results.Ok(ToResponse(result.Request)),
                PaymentRequestActionStatus.RecipientNotFound => Results.NotFound(new PaymentRequestErrorResponse(
                    PaymentRequestErrorCode.RecipientNotFound,
                    "Recipient was not found for the requested currency.",
                    httpContext.TraceIdentifier)),
                PaymentRequestActionStatus.NotFound or PaymentRequestActionStatus.ActorNotAllowed =>
                    NotFound(httpContext, "Payment request not found."),
                _ => Conflict(httpContext, "Payment request action could not be completed.")
            };
        }
        catch (ArgumentException exception)
        {
            return ValidationError(httpContext, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(httpContext, exception.Message);
        }
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirst("sub")?.Value, out userId);

    private static IResult Unauthorized(HttpContext httpContext) => Results.Json(
        new PaymentRequestErrorResponse(
            PaymentRequestErrorCode.Unauthorized,
            "Authenticated user id is missing.",
            httpContext.TraceIdentifier),
        statusCode: StatusCodes.Status401Unauthorized);

    private static IResult NotFound(HttpContext httpContext, string message) => Results.NotFound(
        new PaymentRequestErrorResponse(PaymentRequestErrorCode.NotFound, message, httpContext.TraceIdentifier));

    private static IResult ValidationError(HttpContext httpContext, string message) => Results.BadRequest(
        new PaymentRequestErrorResponse(PaymentRequestErrorCode.ValidationError, message, httpContext.TraceIdentifier));

    private static IResult Conflict(HttpContext httpContext, string message) => Results.Conflict(
        new PaymentRequestErrorResponse(PaymentRequestErrorCode.Conflict, message, httpContext.TraceIdentifier));

    private static RecipientReference CreateRecipientReference(string kind, string value) =>
        kind.Trim().ToLowerInvariant() switch
        {
            "afwal-id" => RecipientReference.FromAfWalId(value),
            "qr" => RecipientReference.FromQrToken(value),
            _ => throw new ArgumentException("Recipient kind must be 'afwal-id' or 'qr'.", nameof(kind))
        };

    private static PaymentRequestHttpResponse ToResponse(PaymentRequestSnapshot snapshot) => new(
        snapshot.Id.Value,
        snapshot.RequesterWalletId.Value,
        snapshot.PayerReference.Kind == RecipientReferenceKind.AfWalId ? "afwal-id" : "qr",
        snapshot.Currency.Code,
        snapshot.AmountMinor,
        snapshot.CorrelationId,
        snapshot.CreatedAtUtc,
        snapshot.ExpiresAtUtc,
        snapshot.Status.ToString(),
        snapshot.AcceptedPayerWalletId?.Value,
        snapshot.AcceptedAtUtc,
        snapshot.TransferId,
        snapshot.ClosedAtUtc);
}
